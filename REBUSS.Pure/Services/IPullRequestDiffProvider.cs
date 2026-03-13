using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services
{
    /// <summary>
    /// Interface for retrieving Pull Request diffs.
    /// </summary>
    public interface IPullRequestDiffProvider
    {
        /// <exception cref="PullRequestNotFoundException">Thrown when PR is not found.</exception>
        Task<PullRequestDiff> GetDiffAsync(int prNumber, CancellationToken cancellationToken = default);

        /// <exception cref="PullRequestNotFoundException">Thrown when PR is not found.</exception>
        /// <exception cref="FileNotFoundInPullRequestException">Thrown when the file does not exist in the PR.</exception>
        Task<PullRequestDiff> GetFileDiffAsync(int prNumber, string path, CancellationToken cancellationToken = default);
    }
}
