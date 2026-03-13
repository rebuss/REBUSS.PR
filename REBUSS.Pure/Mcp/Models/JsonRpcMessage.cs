using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp.Models
{
    /// <summary>
    /// Base class for JSON-RPC messages
    /// </summary>
    public abstract class JsonRpcMessage
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
    }
}
