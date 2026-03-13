using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    public class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }
}
