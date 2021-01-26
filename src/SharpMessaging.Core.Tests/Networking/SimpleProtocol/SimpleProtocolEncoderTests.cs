using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Networking.SimpleProtocol;
using Xunit;

namespace SharpMessaging.Core.Tests.Networking.SimpleProtocol
{
    public class SimpleProtocolEncoderTests
    {
        [Fact]
        public async Task Should_be_able_to_encode_ack()
        {
            var serializer = new JsonTransportSerializer();
            var expectedId = Guid.NewGuid();
            var sender = Substitute.For<ISendState>();

            var sut = new SimpleProtocolEncoder(serializer);
            await sut.EncodeAck(sender, expectedId);

            var sendBuffer = sender.ReceivedCalls().First().GetArguments()[0].As<byte[]>();
            var guidBytes = new byte[16];
            Buffer.BlockCopy(sendBuffer, 1, guidBytes, 0, 16);
            var actual = new Guid(guidBytes);
            actual.Should().Be(expectedId);
        }
    }
}
