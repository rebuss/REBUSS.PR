using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// JSON-RPC Request message
    /// </summary>
    public class JsonRpcRequest : JsonRpcMessage
    {
        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }
}
