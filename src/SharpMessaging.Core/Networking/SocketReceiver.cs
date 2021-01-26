using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;

namespace SharpMessaging.Core.Networking
{
    public class SocketReceiver : IReceiveState
    {
        private readonly SocketAsyncEventArgs _readArgs;
        private readonly SocketAwaitable _readAwaitable;
        private readonly Socket _socket;

        public SocketReceiver(Socket socket, SocketAsyncEventArgs readArgs, SocketAwaitable readAwaitable, byte[] buffer)
        {
            _socket = socket;
            _readArgs = readArgs;
            _readAwaitable = readAwaitable;
            Buffer = buffer;
        }

        public byte[] Buffer { get; set; }
        public int Offset { get; set; }
        public int BytesLeftInBuffer => WriteOffset - Offset;


        public int WriteOffset { get; set; }

        /// <summary>
        ///     Number of bytes that are not currently used in the buffer.
        /// </summary>
        public int BytesUnallocatedInBuffer => Buffer.Length - WriteOffset;

        public async Task EnsureEnoughData(int amountOfBytesToGuarantee)
        {
            if (BytesLeftInBuffer >= amountOfBytesToGuarantee)
                return;

            var bytesReceived = 0;
            while (amountOfBytesToGuarantee > 0)
            {
                // Nearing the end of the buffer, we must move all remaining data to the beginning
                // so that we can continue to receive.
                if (Buffer.Length < Offset + BytesLeftInBuffer + amountOfBytesToGuarantee)
                {
                    if (BytesUnallocatedInBuffer - BytesLeftInBuffer < amountOfBytesToGuarantee)
                        throw new InvalidOperationException("Our buffer is too small.");

                    System.Buffer.BlockCopy(Buffer, Offset, Buffer, 0, BytesLeftInBuffer);
                    Offset = 0;
                }


                _readArgs.SetBuffer(Buffer, WriteOffset, BytesUnallocatedInBuffer);
                var isPending = _socket.ReceiveAsync(_readArgs);
                if (isPending)
                {
                    _readAwaitable.Reset();
                    await _readAwaitable;
                }

                if (_readArgs.BytesTransferred == 0)
                    throw new InvalidOperationException("We got disconnected. TODO: Change this exception.");

                amountOfBytesToGuarantee -= _readArgs.BytesTransferred;
                bytesReceived += _readArgs.BytesTransferred;
            }

            WriteOffset += bytesReceived;
        }

        public void EnsureBufferSize(int dataSize)
        {
            if (Buffer.Length >= dataSize)
                return;

            var buffer = new byte[dataSize];
            if (BytesLeftInBuffer <= 0)
                return;

            System.Buffer.BlockCopy(Buffer, Offset, buffer, 0, BytesLeftInBuffer);
            Offset = 0;
            Buffer = buffer;
        }
    }
}