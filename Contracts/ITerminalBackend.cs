using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevDeck.Contracts
{
    public interface ITerminalBackend : IAsyncDisposable
    {
        event EventHandler<string>? OutputReceived;
        event EventHandler<int>? Exited;

        Task StartAsync(
            string shellPath,
            string arguments,
            string workingDirectory,
            int cols,
            int rows,
            CancellationToken cancellationToken);

        Task WriteAsync(string data, CancellationToken cancellationToken);
        Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken);
        Task StopAsync(bool killProcessTree, CancellationToken cancellationToken);
    }
}
