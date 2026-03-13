using REBUSS.Pure.Services.Models;

namespace REBUSS.Pure.Services.Parsers
{
    public interface IIterationInfoParser
    {
        IterationInfo ParseLast(string json);
    }
}
