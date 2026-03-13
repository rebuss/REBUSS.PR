using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// MCP Tool Definition
    /// </summary>
    public class McpTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("inputSchema")]
        public ToolInputSchema InputSchema { get; set; } = new();
    }
}
