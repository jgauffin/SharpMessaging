using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Messages;

namespace SharpMessaging.Core.Networking.SimpleProtocol
{
    /// <summary>
    /// Used to encode our simple communication protocol.
    /// </summary>
    public class SimpleProtocolEncoder
    {
        private ITransportSerializer _transportSerializer;

        public SimpleProtocolEncoder(ITransportSerializer transportSerializer)
        {
            _transportSerializer = transportSerializer;
        }

        public async Task EncodeHandshake(ISendState sender, int version)
        {
            var buffer = new byte[1];
            buffer[0] = (byte)version;
            await sender.Send(buffer, 0, 1);
        }

        public async Task EncodeAck(ISendState sender, Guid messageId)
        {
            var guidBuffer = messageId.ToByteArray();
            var messageBuffer = new byte[guidBuffer.Length + 1];

            messageBuffer[0] = (byte)TransportMessageType.Ack;
            Buffer.BlockCopy(guidBuffer, 0, messageBuffer, 1, guidBuffer.Length);
            await sender.Send(messageBuffer, 0, messageBuffer.Length);
        }

        public async Task EncodeMessage(ISendState sender, TransportMessage message)
        {
            var contentBytes = _transportSerializer.Serialize(message.Body, message.Headers);
            message.Headers[Headers.ContentLength] = contentBytes.Length.ToString();
            message.Headers[Headers.MessageId] = message.Id.ToString("N");

            var headerBuffer = new byte[65535];
            headerBuffer[0] = (byte)TransportMessageType.Message;
            var offset = 1;
            var lastFeatureOffset = 0;
            foreach (var header in message.Headers)
            {
                lastFeatureOffset = offset;
                offset++; //ignore the feature flag for now.

                var length = Encoding.UTF8.GetBytes(header.Key, 0, header.Key.Length, headerBuffer, offset + 1);
                headerBuffer[offset] = (byte)length;
                offset += length + 1;

                length = Encoding.UTF8.GetBytes(header.Value, 0, header.Value.Length, headerBuffer, offset + 1);
                headerBuffer[offset] = (byte)length;
                offset += length + 1;
            }

            headerBuffer[lastFeatureOffset] = (byte)TransportHeaderFeatureFlags.EndOfHeaders;

            await sender.Send(headerBuffer, 0, offset);
            await sender.Send(contentBytes, 0, contentBytes.Length);
        }

        public async Task EncodeNak(ISendState sender, Guid messageId)
        {
            var buffer = messageId.ToByteArray();
            var messageBuffer = new byte[buffer.Length + 1];

            buffer[0] = (byte)TransportMessageType.Nak;
            Buffer.BlockCopy(buffer, 0, messageBuffer, 1, buffer.Length);
            await sender.Send(messageBuffer, 0, messageBuffer.Length);
        }
    }
}
