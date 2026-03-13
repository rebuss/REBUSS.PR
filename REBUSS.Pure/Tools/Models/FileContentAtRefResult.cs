using System.Text.Json.Serialization;

namespace REBUSS.Pure.Tools.Models
{
    /// <summary>
    /// Structured JSON response model for the get_file_content_at_ref tool.
    /// </summary>
    public class FileContentAtRefResult
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty;

        /// <summary>
        /// Size of the content in bytes (UTF-8 encoded).
        /// </summary>
        [JsonPropertyName("size")]
        public int Size { get; set; }

        /// <summary>
        /// Encoding of <see cref="Content"/>: "utf-8" for text files, "base64" for binary files.
        /// </summary>
        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = "utf-8";

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("isBinary")]
        public bool IsBinary { get; set; }
    }
}
