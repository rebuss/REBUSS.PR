using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using REBUSS.Pure.Mcp;
using REBUSS.Pure.Services;
using REBUSS.Pure.Services.Models;
using REBUSS.Pure.Tools;
using System.Text;
using System.Text.Json;

namespace REBUSS.Pure.Tests.Integration;

/// <summary>
/// End-to-end tests: JSON-RPC request → McpServer → real handler → mocked diff provider → JSON-RPC response.
/// </summary>
public class EndToEndTests
{
    private readonly IPullRequestDiffProvider _diffProvider = Substitute.For<IPullRequestDiffProvider>();

    private McpServer BuildServer(Stream input, Stream output)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.None));
        services.AddSingleton(_diffProvider);
        services.AddSingleton<IMcpToolHandler, GetPullRequestDiffToolHandler>();
        services.AddSingleton(sp =>
            new McpServer(
                sp.GetRequiredService<ILogger<McpServer>>(),
                sp.GetRequiredService<IEnumerable<IMcpToolHandler>>(),
                input,
                output));

        return services.BuildServiceProvider().GetRequiredService<McpServer>();
    }

    private async Task<JsonDocument> SendAsync(string requestJson)
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(requestJson + "\n"));
        using var output = new MemoryStream();

        var server = BuildServer(input, output);
        await server.RunAsync(CancellationToken.None);

        output.Position = 0;
        return JsonDocument.Parse(new StreamReader(output).ReadToEnd().Trim());
    }

    [Fact]
    public async Task FullPipeline_TextFormat_ReturnsFormattedDiff()
    {
        _diffProvider
            .GetDiffAsync(42, Arg.Any<CancellationToken>())
            .Returns(new PullRequestDiff
            {
                Title = "Fix bug",
                Status = "active",
                SourceBranch = "feature/fix",
                TargetBranch = "main",
                Files = new List<FileChange>
                {
                    new() { Path = "/src/App.cs", ChangeType = "edit", Diff = "@@ -1 +1 @@\n-old\n+new" }
                },
                DiffContent = "@@ -1 +1 @@\n-old\n+new"
            });

        var request = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = "e2e-1",
            method = "tools/call",
            @params = new { name = "get_pr_diff", arguments = new { prNumber = 42 } }
        });

        var doc = await SendAsync(request);
        var result = doc.RootElement.GetProperty("result");

        Assert.False(result.GetProperty("isError").GetBoolean());

        var text = result.GetProperty("content")[0].GetProperty("text").GetString()!;
        Assert.Contains("Pull Request #42 Diff", text);
        Assert.DoesNotContain("Fix bug", text);
        Assert.Contains("/src/App.cs", text);
        Assert.Contains("-old", text);
    }

    [Fact]
    public async Task FullPipeline_JsonFormat_ReturnsStructuredResult()
    {
        _diffProvider
            .GetDiffAsync(7, Arg.Any<CancellationToken>())
            .Returns(new PullRequestDiff
            {
                Title = "Add feature",
                Status = "completed",
                SourceBranch = "feature/add",
                TargetBranch = "main",
                SourceRefName = "refs/heads/feature/add",
                TargetRefName = "refs/heads/main",
                Files = new List<FileChange>
                {
                    new() { Path = "/src/New.cs", ChangeType = "add", Diff = "@@ +1 @@\n+line" }
                },
                DiffContent = "@@ +1 @@\n+line"
            });

        var request = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = "e2e-2",
            method = "tools/call",
            @params = new { name = "get_pr_diff", arguments = new { prNumber = 7, format = "json" } }
        });

        var doc = await SendAsync(request);
        var result = doc.RootElement.GetProperty("result");

        Assert.False(result.GetProperty("isError").GetBoolean());

        var innerJson = result.GetProperty("content")[0].GetProperty("text").GetString()!;
        var structured = JsonDocument.Parse(innerJson).RootElement;

        Assert.Equal(7, structured.GetProperty("prNumber").GetInt32());
        Assert.True(structured.TryGetProperty("files", out _));
        Assert.False(structured.TryGetProperty("title", out _));
        Assert.False(structured.TryGetProperty("metadata", out _));
    }

    [Fact]
    public async Task FullPipeline_PrNotFound_ReturnsToolError()
    {
        _diffProvider
            .GetDiffAsync(999, Arg.Any<CancellationToken>())
            .Returns<PullRequestDiff>(x => throw new PullRequestNotFoundException("PR 999 not found"));

        var request = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = "e2e-3",
            method = "tools/call",
            @params = new { name = "get_pr_diff", arguments = new { prNumber = 999 } }
        });

        var doc = await SendAsync(request);
        var result = doc.RootElement.GetProperty("result");

        Assert.True(result.GetProperty("isError").GetBoolean());
        Assert.Contains("not found", result.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task FullPipeline_InitializeThenToolsList_ReturnsToolWithSchema()
    {
        var requests =
            JsonSerializer.Serialize(new { jsonrpc = "2.0", id = "1", method = "initialize", @params = new { } }) + "\n" +
            JsonSerializer.Serialize(new { jsonrpc = "2.0", id = "2", method = "tools/list" }) + "\n";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(requests));
        using var output = new MemoryStream();

        var server = BuildServer(input, output);
        await server.RunAsync(CancellationToken.None);

        output.Position = 0;
        var lines = new StreamReader(output).ReadToEnd()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);

        // Verify tools/list response has schema from the real handler
        var toolsDoc = JsonDocument.Parse(lines[1]);
        var tool = toolsDoc.RootElement.GetProperty("result").GetProperty("tools")[0];
        Assert.Equal("get_pr_diff", tool.GetProperty("name").GetString());

        var props = tool.GetProperty("inputSchema").GetProperty("properties");
        Assert.True(props.TryGetProperty("prNumber", out _));
        Assert.True(props.TryGetProperty("format", out _));
    }
}
