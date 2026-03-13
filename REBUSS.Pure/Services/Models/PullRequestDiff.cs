namespace REBUSS.Pure.Services.Models
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
    }
}
