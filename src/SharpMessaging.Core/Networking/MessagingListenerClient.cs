using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpMessaging.Core.Networking.Helpers;

namespace SharpMessaging.Core.Networking
{
    public class MessagingListenerClient
    {
        private const int SizeHeaderLength = 4;
        private readonly IMessageHandlerInvoker _messageHandlerInvoker;
        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _readArgs = new SocketAsyncEventArgs();
        private readonly SocketAwaitable _readAwaitable;
        private readonly byte[] _readBuffer = new byte[65535];

        public MessagingListenerClient(Socket socket, IMessageHandlerInvoker messageHandlerInvoker)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _messageHandlerInvoker = messageHandlerInvoker;
            _readAwaitable = new SocketAwaitable(_readArgs);
            _readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var transportMessage = await ReceiveSingleMessage();
                await _messageHandlerInvoker.HandleAsync(transportMessage.Body);
            }
        }

        private async Task<TransportMessage> ReceiveSingleMessage()
        {
            var bytesLeft = SizeHeaderLength;
            var bytesRead = 0;
            while (bytesLeft > 0)
            {
                var isPending = _socket.ReceiveAsync(_readArgs);
                if (isPending)
                    await _readAwaitable;

                bytesLeft -= _readArgs.BytesTransferred;
                bytesRead += _readArgs.BytesTransferred;

                if (bytesLeft > 0)
                    _readArgs.SetBuffer(bytesRead, bytesLeft);
            }

            var dataLength = BitConverter.ToInt32(_readBuffer, 0);
            _readArgs.SetBuffer(SizeHeaderLength, dataLength);
            if (bytesRead < dataLength)
            {
                bytesLeft = dataLength - bytesRead - SizeHeaderLength;
                while (bytesLeft > 0)
                {
                    var isPending = _socket.ReceiveAsync(_readArgs);
                    if (isPending)
                        await _readAwaitable;

                    bytesLeft -= _readArgs.BytesTransferred;
                    bytesRead += _readArgs.BytesTransferred;

                    if (bytesLeft > 0)
                        _readArgs.SetBuffer(SizeHeaderLength + bytesRead, bytesLeft);
                }
            }

            var json = Encoding.UTF8.GetString(_readBuffer, SizeHeaderLength, dataLength);
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(json, settings);
            return transportMessage;
        }
    }
}