using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services
{
    /// <summary>
    /// Interface for retrieving full Pull Request metadata.
    /// </summary>
    public interface IPullRequestMetadataProvider
    {
        /// <exception cref="PullRequestNotFoundException">Thrown when PR is not found.</exception>
        Task<FullPullRequestMetadata> GetMetadataAsync(int prNumber, CancellationToken cancellationToken = default);
    }
}
