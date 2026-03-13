using System.Text.Json.Serialization;

namespace REBUSS.Pure.Tools.Models
{
    /// <summary>
    /// Structured JSON response model returned when format is "json" or "structured".
    /// Contains only diff-related data; metadata is served by the get_pr_metadata tool.
    /// </summary>
    public class StructuredDiffResult
    {
        [JsonPropertyName("prNumber")]
        public int PrNumber { get; set; }

        [JsonPropertyName("files")]
        public List<StructuredFileChange> Files { get; set; } = new();
    }

    public class StructuredFileChange
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("changeType")]
        public string ChangeType { get; set; } = string.Empty;

        [JsonPropertyName("diff")]
        public string Diff { get; set; } = string.Empty;
    }
}
