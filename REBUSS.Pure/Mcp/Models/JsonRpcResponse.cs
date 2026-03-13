using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// JSON-RPC Response message
    /// </summary>
    public class JsonRpcResponse : JsonRpcMessage
    {
        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }
    }
}
