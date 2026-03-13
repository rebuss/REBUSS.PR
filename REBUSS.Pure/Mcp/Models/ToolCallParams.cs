using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// Tool Call Parameters
    /// </summary>
    public class ToolCallParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public Dictionary<string, object>? Arguments { get; set; }
    }
}
