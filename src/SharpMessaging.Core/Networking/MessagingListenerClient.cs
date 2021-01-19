using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;

namespace SharpMessaging.Core.Networking
{
    public class MessagingListenerClient
    {
        private readonly IMessageHandlerInvoker _messageHandlerInvoker;
        private readonly TransportProtocol _protocol;
        private readonly SocketAsyncEventArgs _readArgs = new SocketAsyncEventArgs();
        private readonly SocketAwaitable _readAwaitable;
        private readonly byte[] _readBuffer = new byte[65535];
        private readonly DataReceiver _receiver;
        private readonly Socket _socket;
        private readonly ITransportSerializer _serializer = new JsonTransportSerializer();

        public MessagingListenerClient(Socket socket, IMessageHandlerInvoker messageHandlerInvoker)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _messageHandlerInvoker = messageHandlerInvoker;
            _readAwaitable = new SocketAwaitable(_readArgs);
            _readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
            _receiver = new DataReceiver(socket, _readArgs, _readAwaitable, _readBuffer);
            _protocol = new TransportProtocol(_serializer);
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
            return await _protocol.ParseMessage(_receiver);
        }
    }
}