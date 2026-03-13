using System.Text.Json;
using System.Text.Json.Serialization;

namespace REBUSS.Pure.Mcp
{
    /// <summary>
    /// System.Text.Json-backed implementation of <see cref="IJsonRpcSerializer"/>.
    /// Encapsulates the camelCase, no-indent, null-ignoring serializer options
    /// that were previously inlined inside McpServer.
    /// </summary>
    public class SystemTextJsonSerializer : IJsonRpcSerializer
    {
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);
        public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);
    }
}
