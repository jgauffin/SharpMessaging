using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Networking.Helpers;
using Xunit;

namespace SharpMessaging.Core.Tests.Networking
{
    public class TcpMessagingChannelTests : IDisposable
    {
        public TcpMessagingChannelTests()
        {
            _listener = new TcpListener(IPAddress.Any, 0);
            _listener.Start();
            _serverPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _serverListenResult = _listener.BeginAcceptSocket(null, null);
        }

        public void Dispose()
        {
            _serverSocket?.Dispose();

            _listener.Stop();
        }

        private readonly TcpListener _listener;
        private Socket _serverSocket;
        private readonly int _serverPort;
        private readonly IAsyncResult _serverListenResult;

        private async Task<TransportMessage> ReceiveMessageInServer()
        {
            if (_serverSocket == null) _serverSocket = _listener.EndAcceptSocket(_serverListenResult);

            var buffer = new byte[65535];
            var args = new SocketAsyncEventArgs();
            var awaitable = new SocketAwaitable(args);
            var receiver = new DataReceiver(_serverSocket, args, awaitable, buffer);
            var protocol = new TransportProtocol(new JsonTransportSerializer());
            return await protocol.ParseMessage(receiver);
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

            var message = await ReceiveMessageInServer();
            message.Body.Should().Be("Hello world");
        }
    }
}