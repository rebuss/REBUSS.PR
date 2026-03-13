namespace REBUSS.Pure.Services.Common.Models
{
    /// <summary>
    /// Represents a Pull Request diff with all relevant information.
    /// </summary>
    public class PullRequestDiff
    {
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SourceBranch { get; set; } = string.Empty;
        public string TargetBranch { get; set; } = string.Empty;
        public string SourceRefName { get; set; } = string.Empty;
        public string TargetRefName { get; set; } = string.Empty;
        public List<FileChange> Files { get; set; } = new();
        public string DiffContent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a single file change in a PR.
    /// </summary>
    public class FileChange
    {
        public string Path { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string Diff { get; set; } = string.Empty;

        /// <summary>
        /// When set, indicates that diff generation was skipped and explains why
        /// (e.g. "file deleted", "binary file", "generated file", "file renamed", "full file rewrite").
        /// <c>null</c> means the diff was computed normally.
        /// </summary>
        public string? SkipReason { get; set; }
    }
}
