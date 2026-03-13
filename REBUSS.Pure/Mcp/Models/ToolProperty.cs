using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    public class ToolProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Enum { get; set; }

        [JsonPropertyName("default")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Default { get; set; }
    }
}
