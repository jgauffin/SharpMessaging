using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Networking.Messages;
using SharpMessaging.Core.Networking.SimpleProtocol;
using SharpMessaging.Core.Tests.Networking.Helpers;
using Xunit;

namespace SharpMessaging.Core.Tests.Networking.SimpleProtocol
{
    public class SimpleProtocolDecoderTests
    {
        [Fact]
        public async Task Should_be_able_to_decode_an_ack()
        {
            var serializer = new JsonTransportSerializer();
            var expectedId = Guid.NewGuid();
            var state = new TestReceiveState();
            var buf = new byte[17];
            buf[0] = 2; //ack
            var guidBytes = expectedId.ToByteArray();
            Buffer.BlockCopy(guidBytes, 0, buf, 1, 16);
            state.AddBuffer(buf, 0, buf.Length);
            state.WriteOffset = 17;

            var sut = new SimpleProtocolDecoder(serializer);
            var message = await sut.Decode(state);

            message.As<Ack>().MessageId.Should().Be(expectedId);
        }

        [Fact]
        public async Task Should_be_able_to_decode_an_nak()
        {
            var serializer = new JsonTransportSerializer();
            var expectedId = Guid.NewGuid();
            var state = new TestReceiveState();
            var buf = new byte[17];
            buf[0] = 3; //nak
            var guidBytes = expectedId.ToByteArray();
            Buffer.BlockCopy(guidBytes, 0, buf, 1, 16);
            state.AddBuffer(buf, 0, buf.Length);
            state.WriteOffset = 17;

            var sut = new SimpleProtocolDecoder(serializer);
            var message = await sut.Decode(state);

            message.As<Nak>().MessageId.Should().Be(expectedId);
        }

        [Fact]
        public async Task Should_be_able_to_process_two_messages_from_the_same_receive()
        {
            var serializer = new JsonTransportSerializer();
            var encoder = new SimpleProtocolEncoder(serializer);
            var msg1 = new TransportMessage("Test1");
            var msg2 = new TransportMessage("Test2");
            var sender = Substitute.For<ISendState>();
            await encoder.EncodeMessage(sender, msg1);
            await encoder.EncodeMessage(sender, msg2);
            var buffer = new byte[65535];
            var bufferOffset = 0;
            foreach (var call in sender.ReceivedCalls())
            {
                var buf = call.GetArguments()[0].As<byte[]>();
                var offset = call.GetArguments()[1].As<int>();
                var len = call.GetArguments()[2].As<int>();
                Buffer.BlockCopy(buf, offset, buffer, bufferOffset, len);
                bufferOffset += len;
            }
            var state = new TestReceiveState();
            state.AddBuffer(buffer, 0, bufferOffset);
            state.WriteOffset = bufferOffset;

            var sut = new SimpleProtocolDecoder(serializer);
            var actual1 = (await sut.Decode(state)).As<TransportMessage>();
            var actual2 = (await sut.Decode(state)).As<TransportMessage>();

            actual1.Body.Should().Be("Test1");
            actual2.Body.Should().Be("Test2");
        }
    }
}
