using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace REBUSS.Pure.Services;

/// <summary>
/// Produces unified-diff text for a single file given base and target content.
/// Depends on <see cref="IDiffAlgorithm"/> for the line-level edit computation (DIP).
/// </summary>
public class UnifiedDiffBuilder : IUnifiedDiffBuilder
{
    private const int DefaultContextLines = 3;

    private readonly IDiffAlgorithm _diffAlgorithm;
    private readonly ILogger<UnifiedDiffBuilder> _logger;

    public UnifiedDiffBuilder(IDiffAlgorithm diffAlgorithm, ILogger<UnifiedDiffBuilder> logger)
    {
        _diffAlgorithm = diffAlgorithm;
        _logger = logger;
    }

    public string Build(string filePath, string? baseContent, string? targetContent)
    {
        if (baseContent == targetContent)
            return string.Empty;

        var sw = Stopwatch.StartNew();

        var aPath = filePath.TrimStart('/');
        var baseLines = SplitLines(baseContent);
        var targetLines = SplitLines(targetContent);

        var hunks = ComputeHunks(baseLines, targetLines, DefaultContextLines);
        if (hunks.Count == 0)
        {
            _logger.LogDebug(
                "Diff for '{FilePath}': 0 hunks (old={OldLineCount}, new={NewLineCount})",
                aPath, baseLines.Length, targetLines.Length);
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append(BuildDiffHeader(aPath, baseContent, targetContent));
        foreach (var hunk in hunks)
            sb.Append(hunk);

        sw.Stop();

        var result = sb.ToString().TrimEnd();

        _logger.LogDebug(
            "Diff for '{FilePath}': {HunkCount} hunk(s), old={OldLineCount} lines, new={NewLineCount} lines, " +
            "diffLength={DiffLength} chars, {ElapsedMs}ms",
            aPath, hunks.Count, baseLines.Length, targetLines.Length, result.Length, sw.ElapsedMilliseconds);

        if (hunks.Count > 50)
        {
            _logger.LogWarning(
                "Suspicious diff for '{FilePath}': {HunkCount} hunks (possible generated/binary file)",
                aPath, hunks.Count);
        }

        return result;
    }

    internal static string[] SplitLines(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return Array.Empty<string>();
        return content.Replace("\r\n", "\n").Split('\n');
    }

    // --- Header ------------------------------------------------------------------

    private static string BuildDiffHeader(string aPath, string? baseContent, string? targetContent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"diff --git a/{aPath} b/{aPath}");

        AppendFromHeader(sb, aPath, baseContent);
        AppendToHeader(sb, aPath, targetContent);

        return sb.ToString();
    }

    private static void AppendFromHeader(StringBuilder sb, string aPath, string? baseContent)
    {
        if (baseContent is null)
        {
            sb.AppendLine("new file mode 100644");
            sb.AppendLine("--- /dev/null");
        }
        else
            sb.AppendLine($"--- a/{aPath}");
    }

    private static void AppendToHeader(StringBuilder sb, string aPath, string? targetContent)
    {
        if (targetContent is null)
        {
            sb.AppendLine("deleted file mode 100644");
            sb.AppendLine("+++ /dev/null");
        }
        else
            sb.AppendLine($"+++ b/{aPath}");
    }

    // --- Hunk computation --------------------------------------------------------

    private List<string> ComputeHunks(string[] oldLines, string[] newLines, int contextLines)
    {
        var edits = _diffAlgorithm.ComputeEdits(oldLines, newLines);
        var hunks = new List<string>();
        int i = 0;

        while (i < edits.Count)
        {
            if (edits[i].Kind == ' ') { i++; continue; }

            int hunkStart = Math.Max(0, i - contextLines);
            var (hunkEdits, nextI) = CollectHunkEdits(edits, hunkStart, contextLines);
            i = nextI;

            if (hunkEdits.Count > 0)
                hunks.Add(FormatHunk(hunkEdits, oldLines, newLines));
        }

        return hunks;
    }

    private static (List<DiffEdit> HunkEdits, int NextI) CollectHunkEdits(
        IReadOnlyList<DiffEdit> edits, int hunkStart, int contextLines)
    {
        var hunkEdits = new List<DiffEdit>();
        int j = hunkStart;
        int nextI = hunkStart;

        while (j < edits.Count)
        {
            hunkEdits.Add(edits[j]);

            if (edits[j].Kind == ' ')
            {
                var (trailingLen, moreChanges) = CountTrailingContext(edits, j);

                if (!moreChanges || trailingLen > contextLines)
                {
                    int keep = Math.Min(contextLines, trailingLen);
                    for (int x = 1; x < keep && j + x < edits.Count; x++)
                        hunkEdits.Add(edits[j + x]);
                    return (hunkEdits, j + keep);
                }
            }

            j++;
            nextI = j;
        }

        return (hunkEdits, nextI);
    }

    private static (int TrailingLen, bool MoreChanges) CountTrailingContext(
        IReadOnlyList<DiffEdit> edits, int j)
    {
        int trailingEnd = j;
        while (trailingEnd + 1 < edits.Count && edits[trailingEnd + 1].Kind == ' ')
            trailingEnd++;

        return (trailingEnd - j + 1, trailingEnd + 1 < edits.Count);
    }

    // --- Hunk formatting ---------------------------------------------------------

    private static string FormatHunk(List<DiffEdit> hunkEdits, string[] oldLines, string[] newLines)
    {
        int oldStart = hunkEdits.Where(e => e.Kind != '+').Select(e => e.OldIdx + 1).DefaultIfEmpty(1).First();
        int newStart = hunkEdits.Where(e => e.Kind != '-').Select(e => e.NewIdx + 1).DefaultIfEmpty(1).First();
        int oldCount = hunkEdits.Count(e => e.Kind != '+');
        int newCount = hunkEdits.Count(e => e.Kind != '-');

        var sb = new StringBuilder();
        sb.AppendLine($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@");

        foreach (var edit in hunkEdits)
        {
            var lineText = edit.Kind == '+' ? newLines[edit.NewIdx] : oldLines[edit.OldIdx];
            sb.AppendLine($"{edit.Kind}{lineText}");
        }

        return sb.ToString();
    }
}
