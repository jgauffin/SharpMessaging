using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking
{
    /// <summary>
    ///     Uses a binary protocol to transfer messages between two endpoints.
    /// </summary>
    public class TransportProtocol
    {
        private const string HeaderContentLength = "Content-Length";
        private readonly ITransportSerializer _serializer;

        public TransportProtocol(ITransportSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<TransportMessage> ParseMessage(IReceiveState state)
        {
            /* 		Header 
			* Feature flag (1 byte) 1 = int name, 2 =, 128 = End of headers.
			* Name (byte or string (string length (byte) + value)
			* Value (string length, value)
		Content 
			Bytes
            */
            await state.EnsureEnoughData(4);
            var type = (TransportMessageType)state.Buffer[state.Offset++];
            if (type != TransportMessageType.Message)
                throw new NotImplementedException("Not implemented ;(");

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

            if (!headers.TryGetValue(HeaderContentLength, out var contentLengthValue))
                throw new InvalidOperationException($"Expected header '{HeaderContentLength}' to be specified.");

            var contentLength = int.Parse(contentLengthValue);
            state.EnsureBufferSize(contentLength);

            var bytesLeft = contentLength - state.BytesLeftInBuffer;
            await state.EnsureEnoughData(bytesLeft);

            var body = _serializer.Deserialize(state.Buffer, state.Offset, contentLength, headers);
            return new TransportMessage(body)
            {
                Headers = headers
            };
        }

        public async Task Send(ISendState sender, TransportMessage message)
        {
            /*	   MessageType (byte)
		1 = Message
		2 = Ack
		3 = Nak
	   
	   Message
		Header 
			* Feature flag (1 byte) 1 = int name, 2 = 
			* Name (byte or string (string length (byte) + value)
			* Value (string length, value)
		Content 
			Bytes
			*/

            var contentBytes = _serializer.Serialize(message.Body, message.Headers);
            message.Headers[HeaderContentLength] = contentBytes.Length.ToString();

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
    }
}