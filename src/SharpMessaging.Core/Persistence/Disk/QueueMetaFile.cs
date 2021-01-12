using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Persistence.Disk
{
    /// <summary>
    ///     Keeps track of the next record to read in a queue file (so that we can service application restarts)
    /// </summary>
    internal class QueueMetaFile
    {
        private readonly string _fullPath;
        private FileStream _stream;

        /// <summary>
        ///     ssd
        /// </summary>
        /// <param name="queueDirectory">Directory to store the file in. Can be shared by many queues or specific for this queue.</param>
        /// <param name="queueName">File path friendly queue name</param>
        public QueueMetaFile(string queueDirectory, string queueName)
        {
            if (queueDirectory == null) throw new ArgumentNullException(nameof(queueDirectory));
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            _fullPath = Path.Combine(queueDirectory, queueName + ".meta");
        }

        /// <summary>
        ///     Position in file to read next record from.
        /// </summary>
        public int NextRecordPosition { get; private set; }

        /// <summary>
        ///     Open file to find next record to read.
        /// </summary>
        /// <returns></returns>
        public async Task Open()
        {
            _stream = new FileStream(_fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            var buffer = new byte[4];
            var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 4) NextRecordPosition = BitConverter.ToInt32(buffer, 0);
        }

        public async Task WriteNextRecordPosition(int position)
        {
            var buffer = BitConverter.GetBytes(position);

            // Let's always overwrite the position.
            _stream.Position = 0;
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            await _stream.FlushAsync();
        }

        public void Close()
        {
            _stream.Close();
        }
    }
}