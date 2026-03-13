using System.Diagnostics;
using Microsoft.Extensions.Logging;
using REBUSS.Pure.Mcp;
using REBUSS.Pure.Mcp.Models;
using REBUSS.Pure.Services;
using REBUSS.Pure.Services.Models;
using REBUSS.Pure.Tools.Models;
using System.Text.Json;

namespace REBUSS.Pure.Tools
{
    /// <summary>
    /// Handles the execution of the get_pr_diff MCP tool.
    /// Validates input, delegates to <see cref="IPullRequestDiffProvider"/>,
    /// and formats the result as text or structured JSON.
    /// </summary>
    public class GetPullRequestDiffToolHandler : IMcpToolHandler
    {
        private readonly IPullRequestDiffProvider _diffProvider;
        private readonly ILogger<GetPullRequestDiffToolHandler> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public string ToolName => "get_pr_diff";

        public GetPullRequestDiffToolHandler(
            IPullRequestDiffProvider diffProvider,
            ILogger<GetPullRequestDiffToolHandler> logger)
        {
            _diffProvider = diffProvider;
            _logger = logger;
        }

        public McpTool GetToolDefinition() => new()
        {
            Name = ToolName,
            Description = "Retrieves the diff (file changes) for a specific Pull Request from Azure DevOps. " +
                          "Returns the complete diff of all changed files. " +
                          "Use the optional 'format' parameter to choose the output format: " +
                          "'text' (default) returns a human-readable summary followed by unified diff content; " +
                          "'json' or 'structured' returns a structured JSON object with per-file diffs.",
            InputSchema = new ToolInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["prNumber"] = new ToolProperty
                    {
                        Type = "integer",
                        Description = "The Pull Request number/ID to retrieve the diff for"
                    },
                    ["format"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Output format for the diff result. " +
                                      "'text' (default) returns a human-readable summary + unified diff. " +
                                      "'json' or 'structured' returns a structured JSON object with prNumber and per-file diffs.",
                        Enum = new List<string> { "text", "json", "structured" },
                        Default = "text"
                    }
                },
                Required = new List<string> { "prNumber" }
            }
        };

        public async Task<ToolResult> ExecuteAsync(
            Dictionary<string, object>? arguments,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!TryExtractPrNumber(arguments, out var prNumber, out var error))
                {
                    _logger.LogWarning("[{ToolName}] Validation failed: {Error}", ToolName, error);
                    return CreateErrorResult(error);
                }

                var format = ExtractStringArgument(arguments!, "format", "text");

                _logger.LogInformation("[{ToolName}] Entry: PR #{PrNumber}, format={Format}", ToolName, prNumber, format);
                var sw = Stopwatch.StartNew();

                var diff = await _diffProvider.GetDiffAsync(prNumber, cancellationToken);

                var result = format.ToLowerInvariant() switch
                {
                    "json" or "structured" => BuildStructuredResult(prNumber, diff),
                    _ => BuildTextResult(prNumber, diff)
                };

                sw.Stop();

                _logger.LogInformation(
                    "[{ToolName}] Completed: PR #{PrNumber}, format={Format}, {FileCount} file(s), {ResponseLength} chars, {ElapsedMs}ms",
                    ToolName, prNumber, format, diff.Files.Count, result.Content[0].Text.Length, sw.ElapsedMilliseconds);

                return result;
            }
            catch (PullRequestNotFoundException ex)
            {
                _logger.LogWarning(ex, "[{ToolName}] Pull request not found (prNumber={PrNumber})", ToolName, arguments?.GetValueOrDefault("prNumber"));
                return CreateErrorResult($"Pull Request not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{ToolName}] Error (prNumber={PrNumber}, format={Format})",
                    ToolName, arguments?.GetValueOrDefault("prNumber"), arguments?.GetValueOrDefault("format"));
                return CreateErrorResult($"Error retrieving PR diff: {ex.Message}");
            }
        }

        // --- Input extraction -----------------------------------------------------

        private bool TryExtractPrNumber(
            Dictionary<string, object>? arguments,
            out int prNumber,
            out string errorMessage)
        {
            prNumber = 0;
            errorMessage = string.Empty;

            if (arguments == null || !arguments.TryGetValue("prNumber", out var prNumberObj))
            {
                errorMessage = "Missing required parameter: prNumber";
                return false;
            }

            try
            {
                prNumber = prNumberObj is JsonElement jsonElement
                    ? jsonElement.GetInt32()
                    : Convert.ToInt32(prNumberObj);
            }
            catch
            {
                errorMessage = "Invalid prNumber parameter: must be an integer";
                return false;
            }

            if (prNumber <= 0)
            {
                errorMessage = "prNumber must be greater than 0";
                return false;
            }

            return true;
        }

        private static string ExtractStringArgument(
            Dictionary<string, object> arguments, string key, string defaultValue)
        {
            if (!arguments.TryGetValue(key, out var value))
                return defaultValue;

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.String
                    ? jsonElement.GetString() ?? defaultValue
                    : defaultValue;
            }

            return value?.ToString() ?? defaultValue;
        }

        // --- Result builders ------------------------------------------------------

        private static ToolResult BuildTextResult(int prNumber, PullRequestDiff diff)
        {
            var lines = new List<string>
            {
                $"Pull Request #{prNumber} Diff",
                "",
                $"Files Changed: {diff.Files.Count}",
                "",
                "Changed Files:"
            };

            foreach (var file in diff.Files)
                lines.Add($"  - {file.Path} ({file.ChangeType})");

            lines.Add("");
            lines.Add(new string('=', 80));
            lines.Add("DIFF CONTENT:");
            lines.Add(new string('=', 80));
            lines.Add("");
            lines.Add(diff.DiffContent);

            return CreateSuccessResult(string.Join(Environment.NewLine, lines));
        }

        private static ToolResult BuildStructuredResult(int prNumber, PullRequestDiff diff)
        {
            var structured = new StructuredDiffResult
            {
                PrNumber = prNumber,
                Files = diff.Files.Select(f => new StructuredFileChange
                {
                    Path = f.Path,
                    ChangeType = f.ChangeType,
                    Diff = f.Diff
                }).ToList()
            };

            return CreateSuccessResult(JsonSerializer.Serialize(structured, JsonOptions));
        }

        private static ToolResult CreateSuccessResult(string text) => new()
        {
            Content = new List<ContentItem> { new() { Type = "text", Text = text } },
            IsError = false
        };

        private static ToolResult CreateErrorResult(string errorMessage) => new()
        {
            Content = new List<ContentItem> { new() { Type = "text", Text = $"Error: {errorMessage}" } },
            IsError = true
        };
    }
}
