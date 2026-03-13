using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// Tools List Result
    /// </summary>
    public class ToolsListResult
    {
        [JsonPropertyName("tools")]
        public List<McpTool> Tools { get; set; } = new();
    }
}
