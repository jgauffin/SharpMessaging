using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking
{
    /// <summary>
    ///     Information about where
    /// </summary>
    public interface IReceiveState
    {
        /// <summary>
        ///     Part of buffer.
        /// </summary>
        byte[] Buffer { get; }

        /// <summary>
        ///     Bytes left to read from the buffer.
        /// </summary>
        int BytesLeftInBuffer { get; }

        /// <summary>
        ///     Number of bytes that are not currently used in the buffer.
        /// </summary>
        int BytesUnallocatedInBuffer { get; }

        /// <summary>
        ///     Current offset (to read from)
        /// </summary>
        int Offset { get; set; }

        /// <summary>
        ///     Position of the first unallocated byte (where we can continue to add content)
        /// </summary>
        int WriteOffset { get; set;  }

        void EnsureBufferSize(int dataSize);

        Task EnsureEnoughData(int requiredDataLength);
    }
}