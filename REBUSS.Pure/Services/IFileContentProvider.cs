using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services
{
    /// <summary>
    /// Interface for retrieving the full content of a file at a specific Git ref.
    /// </summary>
    public interface IFileContentProvider
    {
        /// <exception cref="FileContentNotFoundException">
        /// Thrown when the file does not exist at the given ref, or the ref is invalid.
        /// </exception>
        Task<FileContent> GetFileContentAsync(string path, string gitRef, CancellationToken cancellationToken = default);
    }
}
