using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;
using SharpMessaging.Core.Networking.Messages;
using SharpMessaging.Core.Networking.SimpleProtocol;

namespace SharpMessaging.Core.Networking
{
    /// <summary>
    ///     Used to send messages over TCP/IP.
    /// </summary>
    public class TcpMessagingClient
    {
        private readonly SocketAsyncEventArgs _writeArgs;
        private readonly SocketAwaitable _writeAwaitable;
        private readonly SimpleProtocolDecoder _decoder;
        private readonly SimpleProtocolEncoder _encoder;
        private string _remoteHost;
        private int _remotePort;
        private SocketSender _sender;
        private readonly ITransportSerializer _serializer = new JsonTransportSerializer();
        private Socket _socket;
        private const int CurrentVersion = 1;
        private readonly SocketAsyncEventArgs _readArgs;
        private readonly SocketAwaitable _readAwaitable;
        private SocketReceiver _receiver;
        private readonly byte[] _readBuffer = new byte[65535];
        private Func<object, Task> _receiverHandler;

        public TcpMessagingClient()
        {
            _writeArgs = new SocketAsyncEventArgs();
            _writeAwaitable = new SocketAwaitable(_writeArgs);

            _readArgs = new SocketAsyncEventArgs();
            _readAwaitable = new SocketAwaitable(_readArgs);

            _encoder = new SimpleProtocolEncoder(_serializer);
            _decoder = new SimpleProtocolDecoder(_serializer);
        }

        /// <summary>
        ///     Checks if we are connected and will attempt to connect if not.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Uses the <c>Connected</c> flag from the socket which only is updated on the last IO operation.
        ///     </para>
        /// </remarks>
        public async Task EnsureConnected()
        {
            if (_socket.Connected) return;

            await Connect();
        }

        public async Task Open(string remoteHost, int port)
        {
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            _remoteHost = remoteHost ?? throw new ArgumentNullException(nameof(remoteHost));
            _remotePort = port;
            await Connect();
        }

        public async Task SendAsync(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (message is TransportMessage existingMessage)
            {
                await _encoder.EncodeMessage(_sender, existingMessage);
            }
            else
            {
                var transportMessage = new TransportMessage(message);
                await _encoder.EncodeMessage(_sender, transportMessage);
            }
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int maxCountToReceive)
        {
            _readArgs.SetBuffer(buffer, offset, maxCountToReceive);
            var isPending = _socket.ReceiveAsync(_readArgs);
            if (isPending)
            {
                _readAwaitable.Reset();
                await _readAwaitable;
            }

            return _readArgs.BytesTransferred;
        }

        protected async Task Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _writeArgs.RemoteEndPoint = new DnsEndPoint(_remoteHost, _remotePort);
            var isPending = _socket.ConnectAsync(_writeArgs);
            if (isPending)
            {
                _writeAwaitable.Reset();
                await _writeAwaitable;
            }

            _sender = new SocketSender(_socket, _writeArgs, _writeAwaitable);
            _receiver = new SocketReceiver(_socket, _readArgs, _readAwaitable, _readBuffer);

            await _encoder.EncodeHandshake(_sender, CurrentVersion);

#pragma warning disable 4014 //Need to run it in a separate task to not block the calling class.
            Task.Run(ReceiveMessages);
#pragma warning restore 4014
        }

        private async Task ReceiveMessages()
        {
            try
            {
                while (_socket.Connected)
                {
                    var message = await _decoder.Decode(_receiver);
                    await _receiverHandler(message);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void SetReceiver(Func<object, Task> callback)
        {
            _receiverHandler = callback;
        }
    }
}