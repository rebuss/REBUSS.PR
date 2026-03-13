using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using REBUSS.Pure.AzureDevOpsIntegration.Services;
using REBUSS.Pure.Services;
using REBUSS.Pure.Services.Parsers;

namespace REBUSS.Pure.Tests.Services;

public class AzureDevOpsDiffProviderTests
{
    private readonly IAzureDevOpsApiClient _apiClient = Substitute.For<IAzureDevOpsApiClient>();
    private readonly AzureDevOpsDiffProvider _provider;

    // Minimal valid JSON responses from Azure DevOps API
    private const string PrDetailsJson = """
        {
            "title": "Fix bug #42",
            "status": "active",
            "sourceRefName": "refs/heads/feature/fix-42",
            "targetRefName": "refs/heads/main"
        }
        """;

    private const string IterationsJson = """
        {
            "value": [
                {
                    "id": 1,
                    "sourceRefCommit": { "commitId": "aaa111" },
                    "commonRefCommit": { "commitId": "bbb222" }
                }
            ]
        }
        """;

    private const string ChangesJson = """
        {
            "changeEntries": [
                {
                    "changeType": "edit",
                    "item": { "path": "/src/File.cs" }
                }
            ]
        }
        """;

    public AzureDevOpsDiffProviderTests()
    {
        _provider = new AzureDevOpsDiffProvider(
            _apiClient,
            new PullRequestMetadataParser(NullLogger<PullRequestMetadataParser>.Instance),
            new IterationInfoParser(NullLogger<IterationInfoParser>.Instance),
            new FileChangesParser(NullLogger<FileChangesParser>.Instance),
            new UnifiedDiffBuilder(new LcsDiffAlgorithm(), NullLogger<UnifiedDiffBuilder>.Instance),
            NullLogger<AzureDevOpsDiffProvider>.Instance);
    }

    [Fact]
    public async Task GetDiffAsync_ReturnsDiff_WithCorrectMetadata()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync("bbb222", "/src/File.cs").Returns("old line");
        _apiClient.GetFileContentAtCommitAsync("aaa111", "/src/File.cs").Returns("new line");

        var result = await _provider.GetDiffAsync(42);

        Assert.Equal("Fix bug #42", result.Title);
        Assert.Equal("active", result.Status);
        Assert.Equal("feature/fix-42", result.SourceBranch);
        Assert.Equal("main", result.TargetBranch);
        Assert.Single(result.Files);
        Assert.Equal("/src/File.cs", result.Files[0].Path);
        Assert.Equal("edit", result.Files[0].ChangeType);
    }

    [Fact]
    public async Task GetDiffAsync_GeneratesUnifiedDiff_ForModifiedFile()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync("bbb222", "/src/File.cs").Returns("old line");
        _apiClient.GetFileContentAtCommitAsync("aaa111", "/src/File.cs").Returns("new line");

        var result = await _provider.GetDiffAsync(42);

        Assert.Contains("-old line", result.DiffContent);
        Assert.Contains("+new line", result.DiffContent);
    }

    [Fact]
    public async Task GetDiffAsync_ThrowsPullRequestNotFound_On404()
    {
        _apiClient.GetPullRequestDetailsAsync(999)
            .ThrowsAsync(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => _provider.GetDiffAsync(999));
    }

    [Fact]
    public async Task GetDiffAsync_HandlesNoIterations()
    {
        _apiClient.GetPullRequestDetailsAsync(42).Returns(PrDetailsJson);
        _apiClient.GetPullRequestIterationsAsync(42).Returns("""{"value":[]}""");

        var result = await _provider.GetDiffAsync(42);

        Assert.Empty(result.Files);
        Assert.Empty(result.DiffContent);
    }

    [Fact]
    public async Task GetDiffAsync_SkipsFolderEntries()
    {
        _apiClient.GetPullRequestDetailsAsync(42).Returns(PrDetailsJson);
        _apiClient.GetPullRequestIterationsAsync(42).Returns(IterationsJson);

        var changesWithFolder = """
            {
                "changeEntries": [
                    {
                        "changeType": "edit",
                        "item": { "path": "/src/", "isFolder": true }
                    },
                    {
                        "changeType": "edit",
                        "item": { "path": "/src/File.cs" }
                    }
                ]
            }
            """;
        _apiClient.GetPullRequestIterationChangesAsync(42, 1).Returns(changesWithFolder);
        _apiClient.GetFileContentAtCommitAsync(Arg.Any<string>(), "/src/File.cs").Returns("content");

        var result = await _provider.GetDiffAsync(42);

        Assert.Single(result.Files);
        Assert.Equal("/src/File.cs", result.Files[0].Path);
    }

    [Fact]
    public async Task GetDiffAsync_UsesFallback_WhenNoCommitSHAs()
    {
        _apiClient.GetPullRequestDetailsAsync(42).Returns(PrDetailsJson);
        // Iterations with no commit SHAs
        _apiClient.GetPullRequestIterationsAsync(42).Returns("""{"value":[{"id":1}]}""");
        _apiClient.GetPullRequestIterationChangesAsync(42, 1).Returns(ChangesJson);

        var result = await _provider.GetDiffAsync(42);

        // Files should be listed but have no per-file diff
        Assert.Single(result.Files);
        Assert.Contains("commit SHAs not resolved", result.DiffContent);
    }

    [Fact]
    public async Task GetDiffAsync_RespectsCancellation()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("x");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _provider.GetDiffAsync(42, cts.Token));
    }

    // --- GetFileDiffAsync ---

    [Fact]
    public async Task GetFileDiffAsync_ReturnsDiff_ForMatchingFile()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync("bbb222", "/src/File.cs").Returns("old line");
        _apiClient.GetFileContentAtCommitAsync("aaa111", "/src/File.cs").Returns("new line");

        var result = await _provider.GetFileDiffAsync(42, "/src/File.cs");

        Assert.Equal("Fix bug #42", result.Title);
        Assert.Single(result.Files);
        Assert.Equal("/src/File.cs", result.Files[0].Path);
        Assert.Contains("-old line", result.DiffContent);
        Assert.Contains("+new line", result.DiffContent);
    }

    [Fact]
    public async Task GetFileDiffAsync_NormalizesLeadingSlash()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync("bbb222", "/src/File.cs").Returns("old");
        _apiClient.GetFileContentAtCommitAsync("aaa111", "/src/File.cs").Returns("new");

        // Pass path without leading slash while data has leading slash
        var result = await _provider.GetFileDiffAsync(42, "src/File.cs");

        Assert.Single(result.Files);
        Assert.Equal("/src/File.cs", result.Files[0].Path);
    }

    [Fact]
    public async Task GetFileDiffAsync_ThrowsFileNotFound_WhenPathDoesNotExist()
    {
        SetupStandardMocks();

        await Assert.ThrowsAsync<FileNotFoundInPullRequestException>(
            () => _provider.GetFileDiffAsync(42, "/src/NonExistent.cs"));
    }

    [Fact]
    public async Task GetFileDiffAsync_ThrowsPullRequestNotFound_On404()
    {
        _apiClient.GetPullRequestDetailsAsync(999)
            .ThrowsAsync(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<PullRequestNotFoundException>(
            () => _provider.GetFileDiffAsync(999, "/src/File.cs"));
    }

    [Fact]
    public async Task GetFileDiffAsync_RespectsCancellation()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("x");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _provider.GetFileDiffAsync(42, "/src/File.cs", cts.Token));
    }

    [Fact]
    public async Task GetFileDiffAsync_IsCaseInsensitive()
    {
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync("bbb222", "/src/File.cs").Returns("old");
        _apiClient.GetFileContentAtCommitAsync("aaa111", "/src/File.cs").Returns("new");

        var result = await _provider.GetFileDiffAsync(42, "/SRC/FILE.CS");

        Assert.Single(result.Files);
    }

    [Fact]
    public async Task GetFileDiffAsync_SharesLogicWithGetDiffAsync()
    {
        // Both methods should fetch the same PR data; verify by calling both and checking metadata
        SetupStandardMocks();
        _apiClient.GetFileContentAtCommitAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("content");

        var fullDiff = await _provider.GetDiffAsync(42);
        var fileDiff = await _provider.GetFileDiffAsync(42, "/src/File.cs");

        Assert.Equal(fullDiff.Title, fileDiff.Title);
        Assert.Equal(fullDiff.Status, fileDiff.Status);
        Assert.Equal(fullDiff.SourceBranch, fileDiff.SourceBranch);
        Assert.Equal(fullDiff.TargetBranch, fileDiff.TargetBranch);
    }

    private void SetupStandardMocks()
    {
        _apiClient.GetPullRequestDetailsAsync(42).Returns(PrDetailsJson);
        _apiClient.GetPullRequestIterationsAsync(42).Returns(IterationsJson);
        _apiClient.GetPullRequestIterationChangesAsync(42, 1).Returns(ChangesJson);
    }
}
