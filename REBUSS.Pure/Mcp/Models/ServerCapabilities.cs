using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// MCP Server Capabilities
    /// </summary>
    public class ServerCapabilities
    {
        [JsonPropertyName("tools")]
        public ToolsCapability? Tools { get; set; }
    }
}
