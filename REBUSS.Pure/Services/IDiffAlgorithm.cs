namespace REBUSS.Pure.Services;

/// <summary>
/// Computes a line-level edit list between two versions of a file.
/// </summary>
public interface IDiffAlgorithm
{
    IReadOnlyList<DiffEdit> ComputeEdits(string[] oldLines, string[] newLines);
}
