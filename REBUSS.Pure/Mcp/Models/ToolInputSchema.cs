using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// JSON Schema for tool input
    /// </summary>
    public class ToolInputSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("properties")]
        public Dictionary<string, ToolProperty> Properties { get; set; } = new();

        [JsonPropertyName("required")]
        public List<string>? Required { get; set; }
    }
}
