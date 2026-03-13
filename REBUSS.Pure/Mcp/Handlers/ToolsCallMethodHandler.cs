using System.Diagnostics;
using Microsoft.Extensions.Logging;
using REBUSS.Pure.Mcp.Models;
using System.Text.Json;

namespace REBUSS.Pure.Mcp.Handlers
{
    /// <summary>
    /// Handles the JSON-RPC <c>tools/call</c> method.
    /// Resolves the named tool from registered <see cref="IMcpToolHandler"/> instances and delegates execution.
    /// </summary>
    public class ToolsCallMethodHandler : IMcpMethodHandler
    {
        private readonly Dictionary<string, IMcpToolHandler> _tools;
        private readonly IJsonRpcSerializer _serializer;
        private readonly ILogger<ToolsCallMethodHandler> _logger;

        public ToolsCallMethodHandler(
            IEnumerable<IMcpToolHandler> tools,
            IJsonRpcSerializer serializer,
            ILogger<ToolsCallMethodHandler> logger)
        {
            _tools = tools.ToDictionary(h => h.ToolName);
            _serializer = serializer;
            _logger = logger;
        }

        public string MethodName => "tools/call";

        public async Task<object> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var paramsElement = request.Params as JsonElement?;
            if (paramsElement == null)
                throw new InvalidOperationException("Missing parameters for tools/call");

            var toolCallParams = _serializer.Deserialize<ToolCallParams>(paramsElement.Value.GetRawText());
            if (toolCallParams == null)
                throw new InvalidOperationException("Invalid tool call parameters");

            if (!_tools.TryGetValue(toolCallParams.Name, out var handler))
            {
                _logger.LogWarning("Unknown tool requested: {ToolName}", toolCallParams.Name);
                throw new InvalidOperationException($"Unknown tool: {toolCallParams.Name}");
            }

            _logger.LogInformation("Dispatching tool {ToolName} with {ArgCount} argument(s)",
                toolCallParams.Name, toolCallParams.Arguments?.Count ?? 0);

            var sw = Stopwatch.StartNew();
            try
            {
                var result = await handler.ExecuteAsync(toolCallParams.Arguments, cancellationToken);
                sw.Stop();

                _logger.LogInformation("Tool {ToolName} completed in {ElapsedMs}ms",
                    toolCallParams.Name, sw.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Tool {ToolName} failed after {ElapsedMs}ms: {ErrorMessage}",
                    toolCallParams.Name, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
