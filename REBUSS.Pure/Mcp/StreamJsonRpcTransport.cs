using System.Text;

namespace REBUSS.Pure.Mcp
{
    /// <summary>
    /// Newline-delimited JSON-RPC transport that wraps a pair of <see cref="Stream"/> objects.
    /// Implements <see cref="IAsyncDisposable"/> so that the underlying StreamReader/StreamWriter
    /// wrappers are released when the server loop exits, without closing the caller-owned streams
    /// (both are opened with <c>leaveOpen: true</c>).
    /// </summary>
    public sealed class StreamJsonRpcTransport : IJsonRpcTransport, IAsyncDisposable
    {
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public StreamJsonRpcTransport(Stream input, Stream output)
        {
            _reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            _writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        }

        public async Task<string?> ReadMessageAsync(CancellationToken cancellationToken)
            => await _reader.ReadLineAsync(cancellationToken);

        public Task WriteMessageAsync(string message, CancellationToken cancellationToken)
            => _writer.WriteLineAsync(message.AsMemory(), cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await _writer.DisposeAsync();
            _reader.Dispose();
        }
    }
}
