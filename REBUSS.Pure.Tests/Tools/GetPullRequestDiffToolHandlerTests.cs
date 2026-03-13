using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using REBUSS.Pure.Services;
using REBUSS.Pure.Services.Models;
using REBUSS.Pure.Tools;

namespace REBUSS.Pure.Tests.Tools;

public class GetPullRequestDiffToolHandlerTests
{
    private readonly IPullRequestDiffProvider _diffProvider = Substitute.For<IPullRequestDiffProvider>();
    private readonly GetPullRequestDiffToolHandler _handler;

    private static readonly PullRequestDiff SampleDiff = new()
    {
        Title = "Fix bug",
        Status = "active",
        SourceBranch = "feature/x",
        TargetBranch = "main",
        SourceRefName = "refs/heads/feature/x",
        TargetRefName = "refs/heads/main",
        Files = new List<FileChange>
        {
            new() { Path = "/src/A.cs", ChangeType = "edit", Diff = "-old\n+new" }
        },
        DiffContent = "-old\n+new"
    };

    public GetPullRequestDiffToolHandlerTests()
    {
        _handler = new GetPullRequestDiffToolHandler(
            _diffProvider,
            NullLogger<GetPullRequestDiffToolHandler>.Instance);
    }

    // --- Happy path ---

    [Fact]
    public async Task ExecuteAsync_ReturnsTextResult_ByDefault()
    {
        _diffProvider.GetDiffAsync(42, Arg.Any<CancellationToken>()).Returns(SampleDiff);

        var args = CreateArgs(42);
        var result = await _handler.ExecuteAsync(args);

        Assert.False(result.IsError);
        var text = result.Content[0].Text;
        Assert.Contains("Pull Request #42 Diff", text);
        Assert.DoesNotContain("Fix bug", text);
        Assert.Contains("-old", text);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("structured")]
    public async Task ExecuteAsync_ReturnsStructuredJson_WhenFormatRequested(string format)
    {
        _diffProvider.GetDiffAsync(42, Arg.Any<CancellationToken>()).Returns(SampleDiff);

        var args = CreateArgs(42, format);
        var result = await _handler.ExecuteAsync(args);

        Assert.False(result.IsError);
        var text = result.Content[0].Text;
        // Should be valid JSON
        var doc = JsonDocument.Parse(text);
        Assert.Equal(42, doc.RootElement.GetProperty("prNumber").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("files", out _));
        Assert.False(doc.RootElement.TryGetProperty("title", out _));
    }

    // --- Validation errors ---

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenArgumentsNull()
    {
        var result = await _handler.ExecuteAsync(null);

        Assert.True(result.IsError);
        Assert.Contains("Missing required parameter", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenPrNumberMissing()
    {
        var result = await _handler.ExecuteAsync(new Dictionary<string, object>());

        Assert.True(result.IsError);
        Assert.Contains("Missing required parameter", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenPrNumberNotInteger()
    {
        var args = new Dictionary<string, object> { ["prNumber"] = "not-a-number" };
        var result = await _handler.ExecuteAsync(args);

        Assert.True(result.IsError);
        Assert.Contains("must be an integer", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenPrNumberZero()
    {
        var args = CreateArgs(0);
        var result = await _handler.ExecuteAsync(args);

        Assert.True(result.IsError);
        Assert.Contains("greater than 0", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenPrNumberNegative()
    {
        var args = CreateArgs(-5);
        var result = await _handler.ExecuteAsync(args);

        Assert.True(result.IsError);
        Assert.Contains("greater than 0", result.Content[0].Text);
    }

    // --- JsonElement input (real MCP scenario) ---

    [Fact]
    public async Task ExecuteAsync_HandlesPrNumberAsJsonElement()
    {
        _diffProvider.GetDiffAsync(42, Arg.Any<CancellationToken>()).Returns(SampleDiff);

        // Simulate what happens when MCP JSON-RPC deserializes arguments
        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(
            """{"prNumber": 42}""")!;
        var result = await _handler.ExecuteAsync(json);

        Assert.False(result.IsError);
    }

    // --- Provider exceptions ---

    [Fact]
    public async Task ExecuteAsync_ReturnsError_WhenPullRequestNotFound()
    {
        _diffProvider.GetDiffAsync(999, Arg.Any<CancellationToken>())
            .ThrowsAsync(new PullRequestNotFoundException("PR #999 not found"));

        var result = await _handler.ExecuteAsync(CreateArgs(999));

        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_OnUnexpectedException()
    {
        _diffProvider.GetDiffAsync(42, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Something broke"));

        var result = await _handler.ExecuteAsync(CreateArgs(42));

        Assert.True(result.IsError);
        Assert.Contains("Something broke", result.Content[0].Text);
    }

    // --- Helpers ---

    private static Dictionary<string, object> CreateArgs(int prNumber, string? format = null)
    {
        var args = new Dictionary<string, object> { ["prNumber"] = prNumber };
        if (format != null)
            args["format"] = format;
        return args;
    }
}
