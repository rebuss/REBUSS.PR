namespace REBUSS.Pure.Services;

/// <summary>
/// Produces unified-diff text for a single file given base and target content.
/// </summary>
public interface IUnifiedDiffBuilder
{
    /// <summary>
    /// Produces a unified-diff string for a single file.
    /// <c>null</c> content means the file did not exist at that commit (add/delete).
    /// Returns <see cref="string.Empty"/> when both sides are identical.
    /// </summary>
    string Build(string filePath, string? baseContent, string? targetContent);
}
