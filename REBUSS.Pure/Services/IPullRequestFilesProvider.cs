using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services
{
    /// <summary>
    /// Interface for retrieving classified file information for a Pull Request.
    /// </summary>
    public interface IPullRequestFilesProvider
    {
        /// <exception cref="PullRequestNotFoundException">Thrown when PR is not found.</exception>
        Task<PullRequestFiles> GetFilesAsync(int prNumber, CancellationToken cancellationToken = default);
    }
}
