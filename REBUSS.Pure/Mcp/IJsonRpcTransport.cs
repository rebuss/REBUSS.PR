namespace REBUSS.Pure.Mcp
{
    /// <summary>
    /// Abstraction over the raw message transport layer (read/write newline-delimited JSON-RPC messages).
    /// Follows DIP: the server depends on this contract rather than StreamReader/StreamWriter directly.
    /// </summary>
    public interface IJsonRpcTransport
    {
        Task<string?> ReadMessageAsync(CancellationToken cancellationToken);
        Task WriteMessageAsync(string message, CancellationToken cancellationToken);
    }
}
