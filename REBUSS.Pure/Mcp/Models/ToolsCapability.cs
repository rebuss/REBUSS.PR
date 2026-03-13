using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    public class ToolsCapability
    {
        [JsonPropertyName("listChanged")]
        public bool ListChanged { get; set; }
    }
}
