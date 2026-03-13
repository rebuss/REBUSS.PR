using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// Initialize Result
    /// </summary>
    public class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2024-11-05";

        [JsonPropertyName("capabilities")]
        public ServerCapabilities Capabilities { get; set; } = new();

        [JsonPropertyName("serverInfo")]
        public ServerInfo ServerInfo { get; set; } = new();
    }
}
