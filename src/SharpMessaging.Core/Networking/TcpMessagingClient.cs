using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        private string _remoteHost;
        private int _remotePort;
        private Socket _socket;

        public TcpMessagingClient()
        {
            _writeArgs = new SocketAsyncEventArgs();
            _writeAwaitable = new SocketAwaitable(_writeArgs);
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

            var msg = new TransportMessage(message);
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            var msgJson = JsonConvert.SerializeObject(msg, settings);
            var bytes = Encoding.UTF8.GetBytes(msgJson);

            var lengthBuffer = BitConverter.GetBytes(bytes.Length);
            _writeArgs.SetBuffer(lengthBuffer, 0, lengthBuffer.Length);
            await _socket.SendAsync(_writeAwaitable);

            _writeArgs.SetBuffer(bytes, 0, bytes.Length);
            await _socket.SendAsync(_writeAwaitable);
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
        }
    }
}