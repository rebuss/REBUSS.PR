using REBUSS.Pure.Mcp.Models;

namespace REBUSS.Pure.Mcp
{
    /// <summary>
    /// Represents an MCP tool that can be listed and executed by the server.
    /// </summary>
    public interface IMcpToolHandler
    {
        string ToolName { get; }
        McpTool GetToolDefinition();
        Task<ToolResult> ExecuteAsync(
            Dictionary<string, object>? arguments,
            CancellationToken cancellationToken = default);
    }
}
