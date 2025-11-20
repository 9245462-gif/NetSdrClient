using System;
using System.Threading;

namespace NetSdrClientApp.Networking
{
    public abstract class NetworkClientBase : IDisposable
    {
        protected CancellationTokenSource? _cts;
        protected bool _disposed = false;

        public bool IsListening => _cts != null && !_cts.Token.IsCancellationRequested;

        protected void StartCancellationToken()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        protected void StopCancellationToken()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        protected void SafeExecute(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
                // Очікуване скасування - не логуємо
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during {operationName}: {ex.Message}");
            }
        }

        protected async Task SafeExecuteAsync(Func<Task> asyncAction, string operationName)
        {
            try
            {
                await asyncAction();
            }
            catch (OperationCanceledException)
            {
                // Очікуване скасування - не логуємо
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during {operationName}: {ex.Message}");
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                StopCancellationToken();
                _disposed = true;
            }
        }
    }
}