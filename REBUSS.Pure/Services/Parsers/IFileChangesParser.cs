using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services.Parsers
{
    public interface IFileChangesParser
    {
        List<FileChange> Parse(string json);
    }
}
