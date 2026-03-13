using REBUSS.Pure.Mcp.Models;

namespace REBUSS.Pure.Mcp
{
    /// <summary>
    /// Abstraction for a single JSON-RPC method handler (e.g. "initialize", "tools/list").
    /// Follows OCP: new methods can be added by registering new handlers without modifying McpServer.
    /// Follows ISP: separate from IMcpToolHandler, which handles tool-level concerns.
    /// </summary>
    public interface IMcpMethodHandler
    {
        string MethodName { get; }
        Task<object> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken);
    }
}
