using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services.Parsers
{
    public interface IPullRequestMetadataParser
    {
        PullRequestMetadata Parse(string json);
        FullPullRequestMetadata ParseFull(string json);
    }
}
