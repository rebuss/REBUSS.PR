namespace REBUSS.Pure.Services.LocalReview
{
    /// <summary>
    /// Thrown when a local git repository root cannot be resolved.
    /// </summary>
    public sealed class LocalRepositoryNotFoundException : Exception
    {
        public LocalRepositoryNotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Thrown when a requested file is not found among the locally changed files.
    /// </summary>
    public sealed class LocalFileNotFoundException : Exception
    {
        public LocalFileNotFoundException(string message) : base(message) { }
    }
}
