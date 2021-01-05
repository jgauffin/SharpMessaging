using System;
using System.IO;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Persistence.Disk
{
    internal class QueueMetaFile
    {
        private readonly string _fullPath;
        private FileStream _stream;

        public QueueMetaFile(string queueDirectory, string queueName)
        {
            _fullPath = Path.Combine(queueDirectory, queueName + ".meta");
        }

        public int NextRecordPosition { get; private set; }

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
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            await _stream.FlushAsync();
        }
    }
}