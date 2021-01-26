using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;

namespace SharpMessaging.Core.Networking
{
    public class SocketSender : ISendState
    {
        private readonly SocketAsyncEventArgs _eventArgs;
        private readonly Socket _socket;
        private readonly SocketAwaitable _socketAwaitable;

        public SocketSender(Socket socket, SocketAsyncEventArgs eventArgs, SocketAwaitable socketAwaitable)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _eventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
            _socketAwaitable = socketAwaitable ?? throw new ArgumentNullException(nameof(socketAwaitable));
        }


        public async Task Send(byte[] buffer, int offset, int length)
        {
            _eventArgs.SetBuffer(buffer, offset, length);
            var isPending = _socket.SendAsync(_eventArgs);
            if (isPending)
            {
                _socketAwaitable.Reset();
                await _socketAwaitable;
            }
        }
    }
}