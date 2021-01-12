using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking.Helpers
{
    /// <summary>
    /// Allows us to use async Task for socket operations.
    /// </summary>
    public sealed class SocketAwaitable : INotifyCompletion
    {
        private static readonly Action Sentinel = () => { };
        private Action _continuation;
        internal readonly SocketAsyncEventArgs _eventArgs;
        private bool _wasCompleted;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            _eventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
            eventArgs.Completed += delegate
            {
                var prev = _continuation ?? Interlocked.CompareExchange(
                               ref _continuation, Sentinel, null);
                prev?.Invoke();
            };
        }

        public bool IsCompleted => _wasCompleted;

        public void OnCompleted(Action continuation)
        {
            if (_continuation == Sentinel ||
                Interlocked.CompareExchange(
                    ref _continuation, continuation, null) == Sentinel)
                Task.Run(continuation);
        }

        internal void Reset()
        {
            _wasCompleted = false;
            _continuation = null;
        }

        public SocketAwaitable GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
            if (_eventArgs.SocketError != SocketError.Success)
                throw new SocketException((int) _eventArgs.SocketError);
        }
    }
}