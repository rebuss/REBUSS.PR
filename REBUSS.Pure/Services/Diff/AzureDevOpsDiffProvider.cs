using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using REBUSS.Pure.AzureDevOpsIntegration.Services;
using REBUSS.Pure.Services.Common;
using REBUSS.Pure.Services.Common.Models;
using REBUSS.Pure.Services.Common.Parsers;
using REBUSS.Pure.Services.FileList.Classification;

namespace REBUSS.Pure.Services.Diff
{
    /// <summary>
    /// Fetches real unified-diff content from Azure DevOps by:
    /// 1. Reading PR details (title, status, refs).
    /// 2. Reading the last iteration to get the base and target commit SHAs.
    /// 3. Enumerating changed files from the iteration changes endpoint.
    /// 4. For each file, fetching raw content at both commits.
    /// 5. Producing standard unified-diff text via <see cref="UnifiedDiffBuilder"/>.
    /// </summary>
    public class AzureDevOpsDiffProvider : IPullRequestDiffProvider
    {
        private const int FullRewriteMinLineCount = 10;

        private readonly IAzureDevOpsApiClient _apiClient;
        private readonly IPullRequestMetadataParser _metadataParser;
        private readonly IIterationInfoParser _iterationParser;
        private readonly IFileChangesParser _changesParser;
        private readonly IUnifiedDiffBuilder _diffBuilder;
        private readonly IFileClassifier _fileClassifier;
        private readonly ILogger<AzureDevOpsDiffProvider> _logger;

        public AzureDevOpsDiffProvider(
            IAzureDevOpsApiClient apiClient,
            IPullRequestMetadataParser metadataParser,
            IIterationInfoParser iterationParser,
            IFileChangesParser changesParser,
            IUnifiedDiffBuilder diffBuilder,
            IFileClassifier fileClassifier,
            ILogger<AzureDevOpsDiffProvider> logger)
        {
            _apiClient = apiClient;
            _metadataParser = metadataParser;
            _iterationParser = iterationParser;
            _changesParser = changesParser;
            _diffBuilder = diffBuilder;
            _fileClassifier = fileClassifier;
            _logger = logger;
        }

        public async Task<PullRequestDiff> GetDiffAsync(int prNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching diff for PR #{PrNumber}", prNumber);
                var sw = Stopwatch.StartNew();

                var (metadata, files, baseCommit, targetCommit) = await FetchPullRequestDataAsync(prNumber);

                _logger.LogInformation(
                    "PR #{PrNumber}: {FileCount} file(s) changed, building diffs (base={BaseCommit}, target={TargetCommit})",
                    prNumber, files.Count,
                    baseCommit?.Length > 7 ? baseCommit[..7] : baseCommit,
                    targetCommit?.Length > 7 ? targetCommit[..7] : targetCommit);

                await BuildFileDiffsAsync(files, baseCommit, targetCommit, cancellationToken);

                var result = BuildDiff(metadata, files, baseCommit, targetCommit);
                sw.Stop();

                _logger.LogInformation(
                    "Diff for PR #{PrNumber} completed: {FileCount} file(s), {DiffLength} chars, {ElapsedMs}ms",
                    prNumber, files.Count, result.DiffContent.Length, sw.ElapsedMilliseconds);

                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Pull Request #{PrNumber} not found", prNumber);
                throw new PullRequestNotFoundException($"Pull Request #{prNumber} not found", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching diff for PR #{PrNumber}", prNumber);
                throw;
            }
        }

        public async Task<PullRequestDiff> GetFileDiffAsync(int prNumber, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching diff for file '{Path}' in PR #{PrNumber}", path, prNumber);
                var sw = Stopwatch.StartNew();

                var (metadata, files, baseCommit, targetCommit) = await FetchPullRequestDataAsync(prNumber);

                var normalizedPath = NormalizePath(path);
                var matchingFiles = files
                    .Where(f => NormalizePath(f.Path).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingFiles.Count == 0)
                {
                    _logger.LogWarning("File '{Path}' not found in PR #{PrNumber}", path, prNumber);
                    throw new FileNotFoundInPullRequestException(
                        $"File '{path}' not found in Pull Request #{prNumber}");
                }

                await BuildFileDiffsAsync(matchingFiles, baseCommit, targetCommit, cancellationToken);

                var result = BuildDiff(metadata, matchingFiles, baseCommit, targetCommit);
                sw.Stop();

                _logger.LogInformation(
                    "File diff for '{Path}' in PR #{PrNumber} completed: {DiffLength} chars, {ElapsedMs}ms",
                    path, prNumber, result.DiffContent.Length, sw.ElapsedMilliseconds);

                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Pull Request #{PrNumber} not found", prNumber);
                throw new PullRequestNotFoundException($"Pull Request #{prNumber} not found", ex);
            }
            catch (FileNotFoundInPullRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching diff for file '{Path}' in PR #{PrNumber}", path, prNumber);
                throw;
            }
        }

        private async Task<(PullRequestMetadata metadata, List<FileChange> files, string baseCommit, string targetCommit)>
            FetchPullRequestDataAsync(int prNumber)
        {
            var metadata  = _metadataParser.Parse(await _apiClient.GetPullRequestDetailsAsync(prNumber));
            var iteration = _iterationParser.ParseLast(await _apiClient.GetPullRequestIterationsAsync(prNumber));
            var files     = await FetchFileChangesAsync(prNumber, iteration.Id);

            return (metadata, files, iteration.BaseCommit, iteration.TargetCommit);
        }

        private static string NormalizePath(string path) => path.TrimStart('/');

        private async Task<List<FileChange>> FetchFileChangesAsync(int prNumber, int iterationId)
        {
            var changesJson = iterationId > 0
                ? await _apiClient.GetPullRequestIterationChangesAsync(prNumber, iterationId)
                : "{}";

            return _changesParser.Parse(changesJson);
        }

        private async Task BuildFileDiffsAsync(
            List<FileChange> files,
            string baseCommit,
            string targetCommit,
            CancellationToken cancellationToken)
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(baseCommit) || string.IsNullOrEmpty(targetCommit))
                {
                    _logger.LogDebug(
                        "Skipping diff for '{FilePath}': commit SHAs not resolved (base={BaseCommit}, target={TargetCommit})",
                        file.Path, baseCommit ?? "<null>", targetCommit ?? "<null>");
                    continue;
                }

                var skipReason = GetSkipReason(file);
                if (skipReason is not null)
                {
                    file.SkipReason = skipReason;
                    file.Diff = BuildSkippedDiffMarker(file.Path, file.ChangeType, skipReason);
                    _logger.LogDebug(
                        "Skipping diff for '{FilePath}': {SkipReason}",
                        file.Path, skipReason);
                    continue;
                }

                var fileSw = Stopwatch.StartNew();

                var baseContent   = await _apiClient.GetFileContentAtCommitAsync(baseCommit,   file.Path);
                var targetContent = await _apiClient.GetFileContentAtCommitAsync(targetCommit, file.Path);
                file.Diff = _diffBuilder.Build(file.Path, baseContent, targetContent);

                if (IsFullFileRewrite(baseContent, targetContent, file.Diff))
                {
                    file.SkipReason = "full file rewrite";
                    file.Diff = BuildSkippedDiffMarker(file.Path, file.ChangeType, "full file rewrite");
                    _logger.LogDebug(
                        "Replaced diff for '{FilePath}': detected full file rewrite",
                        file.Path);
                }

                fileSw.Stop();

                _logger.LogDebug(
                    "Built diff for '{FilePath}' ({ChangeType}): {DiffLength} chars, {ElapsedMs}ms",
                    file.Path, file.ChangeType, file.Diff?.Length ?? 0, fileSw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Returns a skip reason if the file should not have its diff computed,
        /// or <c>null</c> if normal diff logic should proceed.
        /// </summary>
        internal string? GetSkipReason(FileChange file)
        {
            if (string.Equals(file.ChangeType, "delete", StringComparison.OrdinalIgnoreCase))
                return "file deleted";

            if (string.Equals(file.ChangeType, "rename", StringComparison.OrdinalIgnoreCase))
                return "file renamed";

            var classification = _fileClassifier.Classify(file.Path);

            if (classification.IsBinary)
                return "binary file";

            if (classification.IsGenerated)
                return "generated file";

            return null;
        }

        /// <summary>
        /// Detects a full-file rewrite: both contents are non-trivial but the diff
        /// contains zero context (unchanged) lines, indicating every line changed.
        /// </summary>
        internal static bool IsFullFileRewrite(string? baseContent, string? targetContent, string diff)
        {
            if (string.IsNullOrEmpty(baseContent) || string.IsNullOrEmpty(targetContent))
                return false;

            if (string.IsNullOrEmpty(diff))
                return false;

            var oldLineCount = baseContent.Replace("\r\n", "\n").Split('\n').Length;
            var newLineCount = targetContent.Replace("\r\n", "\n").Split('\n').Length;

            if (oldLineCount < FullRewriteMinLineCount && newLineCount < FullRewriteMinLineCount)
                return false;

            var diffLines = diff.Split('\n');
            bool inHunk = false;

            foreach (var line in diffLines)
            {
                if (line.StartsWith("@@"))
                {
                    inHunk = true;
                    continue;
                }

                if (inHunk && line.Length > 0 && line[0] == ' ')
                    return false;
            }

            return inHunk;
        }

        private static string BuildSkippedDiffMarker(string path, string changeType, string reason)
        {
            var p = path.TrimStart('/');
            var sb = new StringBuilder();
            sb.AppendLine($"diff --git a/{p} b/{p}");
            sb.AppendLine($"--- a/{p}");
            sb.AppendLine($"+++ b/{p}");
            sb.Append($"# {changeType} — {reason}, diff skipped");
            return sb.ToString();
        }

        private static PullRequestDiff BuildDiff(
            PullRequestMetadata metadata,
            List<FileChange> files,
            string baseCommit,
            string targetCommit)
        {
            return new PullRequestDiff
            {
                Title         = metadata.Title,
                Status        = metadata.Status,
                SourceBranch  = metadata.SourceBranch,
                TargetBranch  = metadata.TargetBranch,
                SourceRefName = metadata.SourceRefName,
                TargetRefName = metadata.TargetRefName,
                Files         = files,
                DiffContent   = BuildDiffContent(files, baseCommit, targetCommit)
            };
        }

        private static string BuildDiffContent(List<FileChange> files, string baseCommit, string targetCommit)
        {
            var diffSections = files
                .Where(f => !string.IsNullOrEmpty(f.Diff))
                .Select(f => f.Diff)
                .ToList();

            if (diffSections.Count > 0)
                return string.Join("\n", diffSections);

            var noCommitShas = string.IsNullOrEmpty(baseCommit) || string.IsNullOrEmpty(targetCommit);
            return GenerateFallbackDiff(files, noCommitShas);
        }

        private static string GenerateFallbackDiff(List<FileChange> files, bool noCommitShas)
        {
            if (files.Count == 0) return string.Empty;

            var reason = noCommitShas ? "commit SHAs not resolved from iteration" : "file content unavailable";
            var sb = new StringBuilder();

            foreach (var f in files)
            {
                var p = f.Path.TrimStart('/');
                sb.AppendLine($"diff --git a/{p} b/{p}");
                sb.AppendLine($"--- a/{p}");
                sb.AppendLine($"+++ b/{p}");
                sb.AppendLine($"# {f.ChangeType} ({reason})");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
