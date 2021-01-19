using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Tests.Networking.Helpers;
using Xunit;

namespace SharpMessaging.Core.Tests.Networking
{
    public class TransportProtocolTests
    {
        private bool FindHeader(string name, string value, byte[] headerBuffer)
        {
            var buffer = new byte[512];
            var offset = 1;
            offset += Encoding.UTF8.GetBytes(name, 0, name.Length, buffer, 1);
            buffer[0] = (byte)name.Length;

            buffer[offset++] = (byte)value.Length;
            offset += Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
            for (var i = 0; i < headerBuffer.Length; i++)
            {
                if (headerBuffer[i] != 0 && headerBuffer[i] != 128) // feature flags.
                    continue;

                var thisHeader = headerBuffer.Skip(i).Take(255).ToArray();
                i++;
                var isFound = false;
                for (var j = 0; j < offset; j++)
                {
                    if (headerBuffer[i + j] != buffer[j])
                    {
                        isFound = false;
                        break;
                    }

                    isFound = true;
                }

                if (isFound)
                    return true;
            }

            return false;
        }

        [Fact]
        public async Task Should_be_able_to_deserialize_content()
        {
        }

        [Fact]
        public async Task Should_be_able_to_encode_and_decode()
        {
            var serialiser = new JsonTransportSerializer();
            var sender = Substitute.For<ISendState>();
            var msg = new TransportMessage("Hello world");

            var sut = new TransportProtocol(serialiser);
            await sut.Send(sender, msg);

            var headerCall = sender.ReceivedCalls().First().GetArguments();
            var bodyCall = sender.ReceivedCalls().Last().GetArguments();

            var receiveState = new TestReceiveState();
            receiveState.AddBuffer(headerCall[0].As<byte[]>(), 0, headerCall[2].As<int>());
            receiveState.AddBuffer(bodyCall[0].As<byte[]>(), 0, bodyCall[2].As<int>());
            var actual = await sut.ParseMessage(receiveState);
            actual.Body.Should().Be("Hello world");
        }

        [Fact]
        public async Task Should_be_able_to_serialize_content()
        {
            var serialiser = new JsonTransportSerializer();
            var sender = Substitute.For<ISendState>();
            var msg = new TransportMessage("Hello world");

            var sut = new TransportProtocol(serialiser);
            await sut.Send(sender, msg);

            var args = sender.ReceivedCalls().First().GetArguments();
            var headerBytes = args[0].As<byte[]>();
            FindHeader("Content-Type", "application/json", headerBytes).Should().BeTrue();
            FindHeader("Content-Length", msg.Headers["Content-Length"], headerBytes).Should().BeTrue();
            FindHeader("Type-Name", msg.Headers["Type-Name"], headerBytes).Should().BeTrue();

            args = sender.ReceivedCalls().Last().GetArguments();
            var len = (int)args[2];
            Encoding.UTF8.GetString(args[0].As<byte[]>(), 0, len).Should().Be("\"Hello world\"");
        }
    }
}