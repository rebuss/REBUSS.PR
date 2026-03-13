using REBUSS.Pure.Mcp.Models;

namespace REBUSS.Pure.Mcp.Handlers
{
    /// <summary>
    /// Handles the JSON-RPC <c>tools/list</c> method.
    /// Returns the definitions of all registered <see cref="IMcpToolHandler"/> instances.
    /// </summary>
    public class ToolsListMethodHandler : IMcpMethodHandler
    {
        private readonly IEnumerable<IMcpToolHandler> _tools;

        public ToolsListMethodHandler(IEnumerable<IMcpToolHandler> tools) => _tools = tools;

        public string MethodName => "tools/list";

        public Task<object> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var result = new ToolsListResult
            {
                Tools = _tools.Select(h => h.GetToolDefinition()).ToList()
            };

            return Task.FromResult<object>(result);
        }
    }
}
