using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;

namespace SharpMessaging.Core.Networking
{
    /// <summary>
    ///     Used to send messages over TCP/IP.
    /// </summary>
    public class TcpMessagingClient
    {
        private readonly SocketAsyncEventArgs _writeArgs;
        private readonly SocketAwaitable _writeAwaitable;
        private readonly TransportProtocol _protocol;
        private string _remoteHost;
        private int _remotePort;
        private SockerSender _sender;
        private readonly ITransportSerializer _serializer = new JsonTransportSerializer();
        private Socket _socket;

        public TcpMessagingClient()
        {
            _writeArgs = new SocketAsyncEventArgs();
            _writeAwaitable = new SocketAwaitable(_writeArgs);
            _protocol = new TransportProtocol(_serializer);
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

            var transportMessage = new TransportMessage(message);
            await _protocol.Send(_sender, transportMessage);
        }

        protected async Task Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _writeArgs.RemoteEndPoint = new DnsEndPoint(_remoteHost, _remotePort);
            var isPending = _socket.ConnectAsync(_writeArgs);
            if (!isPending)
                return;

            _writeAwaitable.Reset();
            await _writeAwaitable;

            _sender = new SockerSender(_socket, _writeArgs, _writeAwaitable);
        }
    }
}