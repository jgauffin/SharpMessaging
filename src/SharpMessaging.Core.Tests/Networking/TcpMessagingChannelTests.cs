using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using SharpMessaging.Core.Networking;
using Xunit;

namespace SharpMessaging.Core.Tests.Networking
{
    public class TcpMessagingChannelTests : IDisposable
    {
        private readonly TcpListener _listener;
        private Socket _serverSocket;
        private readonly int _serverPort;
        private readonly IAsyncResult _serverListenResult;

        public TcpMessagingChannelTests()
        {
            _listener = new TcpListener(IPAddress.Any, 0);
            _listener.Start();
            _serverPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _serverListenResult = _listener.BeginAcceptSocket(null, null);
        }

        public void Dispose()
        {
            if (_serverSocket != null)
                _serverSocket.Dispose();

            _listener.Stop();
        }

        private TransportMessage ReceiveMessageInServer()
        {
            if (_serverSocket == null) _serverSocket = _listener.EndAcceptSocket(_serverListenResult);

            var buffer = new byte[65535];
            var bytes = _serverSocket.Receive(buffer, 0, 4, SocketFlags.None);
            if (bytes != 4)
                throw new InvalidOperationException("Failed to receive header.");

            var dataLength = BitConverter.ToInt32(buffer, 0);
            if (buffer.Length < dataLength)
                buffer = new byte[dataLength];

            var bytesLeft = dataLength;
            var offset = 0;
            while (bytesLeft > 0)
            {
                var read = _serverSocket.Receive(buffer, offset, bytesLeft, SocketFlags.None);
                bytesLeft -= read;
                offset += read;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, dataLength);
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            var transportMessage = JsonConvert.DeserializeObject<TransportMessage>(json, settings);
            return transportMessage;
        }

        [Fact]
        public async Task Should_be_able_to_connect()
        {
            var sut = new TcpMessagingClient();

            await sut.Open("localhost", _serverPort);
        }

        [Fact]
        public async Task Should_be_able_to_send_a_message()
        {
            var sut = new TcpMessagingClient();
            await sut.Open("localhost", _serverPort);

            await sut.SendAsync("Hello world");

            var message = ReceiveMessageInServer();
            message.Body.Should().Be("Hello world");
        }
    }
}