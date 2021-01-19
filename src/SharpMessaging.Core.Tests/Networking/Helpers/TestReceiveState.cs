using System;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking;

namespace SharpMessaging.Core.Tests.Networking.Helpers
{
    internal class TestReceiveState : IReceiveState
    {
        private readonly byte[] _buffer = new byte[65535];

        public TestReceiveState()
        {
            Buffer = _buffer;
        }

        public byte[] Buffer { get; set; }
        public int Offset { get; set; }
        public int BytesLeftInBuffer => WriteOffset - Offset;
        public int WriteOffset { get; set; }

        public int BytesUnallocatedInBuffer => Buffer.Length - WriteOffset;

        public Task EnsureEnoughData(int requiredDataLength)
        {
            if (BytesLeftInBuffer < requiredDataLength)
                throw new InvalidOperationException("Not enough data");
            return Task.CompletedTask;
        }

        public void EnsureBufferSize(int dataSize)
        {
            if (BytesLeftInBuffer < dataSize)
                throw new InvalidOperationException("Not enough data");
        }

        public void AddBuffer(byte[] buffer, int offset, int count)
        {
            System.Buffer.BlockCopy(buffer, offset, _buffer, WriteOffset, count);
            WriteOffset += count;
        }
    }
}