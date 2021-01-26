using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;
using SharpMessaging.Core.Networking.Messages;
using SharpMessaging.Core.Networking.SimpleProtocol;

namespace SharpMessaging.Core.Networking
{
    public class MessagingListenerClient
    {
        private readonly IMessageHandlerInvoker _messageHandlerInvoker;
        private readonly SimpleProtocolEncoder _encoder;
        private readonly SimpleProtocolDecoder _decoder;
        private readonly SocketAsyncEventArgs _readArgs = new SocketAsyncEventArgs();
        private readonly SocketAwaitable _readAwaitable;
        private readonly SocketAwaitable _writeAwaitable;
        private readonly SocketAsyncEventArgs _writeArgs = new SocketAsyncEventArgs();
        private readonly byte[] _readBuffer = new byte[65535];
        private readonly SocketReceiver _receiver;
        private readonly ISendState _sendState;
        private readonly Socket _socket;
        private readonly ITransportSerializer _serializer = new JsonTransportSerializer();
        private const int CurrentVersion = 1;

        public MessagingListenerClient(Socket socket, IMessageHandlerInvoker messageHandlerInvoker)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _messageHandlerInvoker = messageHandlerInvoker;

            // For inbound data
            _readAwaitable = new SocketAwaitable(_readArgs);
            _readArgs.SetBuffer(_readBuffer, 0, _readBuffer.Length);
            _receiver = new SocketReceiver(socket, _readArgs, _readAwaitable, _readBuffer);
            _decoder = new SimpleProtocolDecoder(_serializer);

            // For outbound data
            _encoder = new SimpleProtocolEncoder(_serializer);
            _writeArgs = new SocketAsyncEventArgs();
            _writeAwaitable = new SocketAwaitable(_writeArgs);
            _sendState = new SocketSender(_socket, _writeArgs, _writeAwaitable);
        }

        public async Task Run(CancellationToken token)
        {
            await ReceiveHandshake();
            while (!token.IsCancellationRequested)
            {
                var messageId = Guid.Empty;
                try
                {
                    var transportMessage = await ReceiveSingleMessage();
                    messageId = transportMessage.Id;
                    await _messageHandlerInvoker.HandleAsync(transportMessage.Body);
                    await _encoder.EncodeAck(_sendState, transportMessage.Id);
                }
                catch
                {
                    await _encoder.EncodeNak(_sendState, messageId);
                }
            }
        }

        private async Task ReceiveHandshake()
        {
            var remoteVersion = await _decoder.DecodeHandshake(_receiver);
            if (remoteVersion > CurrentVersion)
                throw new NotSupportedException(
                    $"The remote endpoint is using a higher protocol version (v{remoteVersion}) than the one we support.");
        }

        private async Task<TransportMessage> ReceiveSingleMessage()
        {
            var message = await _decoder.Decode(_receiver);
            if (message is TransportMessage t)
                return t;

            throw new NotSupportedException("Not implemented just yet.");
        }
    }
}