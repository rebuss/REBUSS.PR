namespace REBUSS.Pure.Services.Models
{
    /// <summary>
    /// Parsed metadata from the Azure DevOps pull request details endpoint.
    /// </summary>
    public sealed record PullRequestMetadata(
        string Title,
        string Status,
        string SourceBranch,
        string TargetBranch,
        string SourceRefName,
        string TargetRefName);
}
