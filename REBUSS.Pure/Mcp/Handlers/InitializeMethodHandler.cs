using REBUSS.Pure.Mcp.Models;

namespace REBUSS.Pure.Mcp.Handlers
{
    /// <summary>
    /// Handles the JSON-RPC <c>initialize</c> method.
    /// Returns protocol version, capabilities, and server info.
    /// </summary>
    public class InitializeMethodHandler : IMcpMethodHandler
    {
        public string MethodName => "initialize";

        public Task<object> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var result = new InitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability { ListChanged = false }
                },
                ServerInfo = new ServerInfo
                {
                    Name = "REBUSS.Pure",
                    Version = "1.0.0"
                }
            };

            return Task.FromResult<object>(result);
        }
    }
}
