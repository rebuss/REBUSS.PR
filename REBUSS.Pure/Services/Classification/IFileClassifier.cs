namespace REBUSS.Pure.Services.Classification
{
    /// <summary>
    /// Classifies a file by its path to determine type, priority, and category.
    /// </summary>
    public interface IFileClassifier
    {
        FileClassification Classify(string path);
    }
}
