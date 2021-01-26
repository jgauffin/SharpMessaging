using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Messages;

namespace SharpMessaging.Core.Networking.SimpleProtocol
{
    public class SimpleProtocolDecoder
    {
        private ITransportSerializer _transportSerializer;

        public SimpleProtocolDecoder(ITransportSerializer transportSerializer)
        {
            _transportSerializer = transportSerializer ?? throw new ArgumentNullException(nameof(transportSerializer));
        }

        public async Task<object> Decode(IReceiveState state)
        {
            await state.EnsureEnoughData(4);
            var type = (TransportMessageType)state.Buffer[state.Offset++];
            switch (type)
            {
                case TransportMessageType.Message:
                    return await DecodeMessage(state);
                case TransportMessageType.Ack:
                    return await DecodeAck(state);
                case TransportMessageType.Nak:
                    return await DecodeNak(state);
                default:
                    throw new NotSupportedException($"Have not implemented {type} just yet.");
            }
        }

        private async Task<object> DecodeNak(IReceiveState state)
        {
            await state.EnsureEnoughData(16);

            var guidBytes = new byte[16];
            Buffer.BlockCopy(state.Buffer, state.Offset, guidBytes, 0, guidBytes.Length);
            var guid = new Guid(guidBytes);
            return new Nak(guid);
        }

        private async Task<object> DecodeAck(IReceiveState state)
        {
            await state.EnsureEnoughData(16);

            var guidBytes = new byte[16];
            Buffer.BlockCopy(state.Buffer, state.Offset, guidBytes, 0, guidBytes.Length);
            var guid = new Guid(guidBytes);
            return new Ack(guid);
        }

        private async Task<TransportMessage> DecodeMessage(IReceiveState state)
        {
            var headers = new Dictionary<string, string>();
            while (true)
            {
                if (state.BytesLeftInBuffer < 3) await state.EnsureEnoughData(3);

                var featureFlag = state.Buffer[state.Offset++];

                var stringLength = state.Buffer[state.Offset++];
                await state.EnsureEnoughData(stringLength);
                var headerName = Encoding.UTF8.GetString(state.Buffer, state.Offset, stringLength);
                state.Offset += stringLength;

                stringLength = state.Buffer[state.Offset++];
                await state.EnsureEnoughData(stringLength);
                var headerValue = Encoding.UTF8.GetString(state.Buffer, state.Offset, stringLength);
                state.Offset += stringLength;

                headers.Add(headerName, headerValue);

                if ((featureFlag & (byte)TransportHeaderFeatureFlags.EndOfHeaders) != 0)
                    break;
            }

            if (!headers.TryGetValue(Headers.ContentLength, out var contentLengthValue))
                throw new InvalidOperationException($"Expected header '{Headers.ContentLength}' to be specified.");

            Guid messageId;
            if (headers.TryGetValue(Headers.MessageId, out var messageIdStr))
            {
                messageId = Guid.Parse(messageIdStr);
            }
            else
                throw new InvalidOperationException("All messages must have an id.");

            var contentLength = int.Parse(contentLengthValue);
            state.EnsureBufferSize(contentLength);

            var bytesLeft = contentLength - state.BytesLeftInBuffer;
            await state.EnsureEnoughData(bytesLeft);

            var body = _transportSerializer.Deserialize(state.Buffer, state.Offset, contentLength, headers);
            state.Offset += contentLength;

            return new TransportMessage(body)
            {
                Id = messageId,
                Headers = headers
            };
        }

        public async Task<byte> DecodeHandshake(IReceiveState receiver)
        {
            await receiver.EnsureEnoughData(1);
            return receiver.Buffer[receiver.Offset++];
        }
    }
}
