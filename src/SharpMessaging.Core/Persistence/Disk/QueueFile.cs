using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharpMessaging.Core.Persistence.Disk
{
    /// <summary>
    ///     Keeps track of a specific file.
    /// </summary>
    public class QueueFile
    {
        private readonly string _fileName;
        private readonly QueueMetaFile _metaFile;
        private readonly string _queueDirectory;
        private string _fullPath;
        private FileStream _readStream;
        private FileStream _writeStream;

        public QueueFile(string queueDirectory, string fileName)
        {
            _queueDirectory = queueDirectory ?? throw new ArgumentNullException(nameof(queueDirectory));
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _metaFile = new QueueMetaFile(queueDirectory, Path.GetFileNameWithoutExtension(fileName));
        }

        /// <summary>
        ///     Current size of the file.
        /// </summary>
        public int FileSize => _writeStream == null ? 0 : (int)_writeStream.Length;

        /// <summary>
        ///     Number of records available for dequeue.
        /// </summary>
        public int RecordCount { get; private set; }

        public void Close()
        {
            _readStream.Close();
            _writeStream.Close();
            _metaFile.Close();
        }

        public void Delete()
        {
            File.Delete(_fullPath);
        }


        public async Task<DequeuedMessage> Dequeue()
        {
            var originalPosition = _readStream.Position;
            var len = _readStream.ReadByte();
            if (len == -1) return null;

            var buffer = new byte[1000];
            var bytesRead = await _readStream.ReadAsync(buffer, 0, len);
            if (bytesRead != len)
                throw new InvalidOperationException("Could not read a complete type name");

            var typeName = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var typeToDeserialize = Type.GetType(typeName, true);

            var dataLengthBuffer = new byte[2];
            bytesRead = _readStream.Read(dataLengthBuffer, 0, dataLengthBuffer.Length);
            var dataLength = (int)BitConverter.ToUInt16(dataLengthBuffer, 0);

            var jsonBuffer = new byte[dataLength];
            bytesRead = await _readStream.ReadAsync(jsonBuffer, 0, dataLength);
            if (bytesRead != dataLength)
                throw new InvalidOperationException("Failed to read JSON document");

            var json = Encoding.UTF8.GetString(jsonBuffer, 0, bytesRead);
            var body = JsonConvert.DeserializeObject(json, typeToDeserialize);
            return new DequeuedMessage(body, async () =>
            {
                await _metaFile.WriteNextRecordPosition((int)_readStream.Position);
                RecordCount--;
            }, () =>
            {
                _readStream.Position = originalPosition;
                return Task.CompletedTask;
            });
        }

        public async Task Enqueue(object message)
        {
            /*TypeNameLength (byte)
 TypeName
 DataLength (short)
 Data
 
 */
            var name = message.GetType().AssemblyQualifiedName;
            var bytes = Encoding.UTF8.GetBytes(name);
            _writeStream.WriteByte((byte)bytes.Length);
            _writeStream.Write(bytes, 0, bytes.Length);

            var json = JsonConvert.SerializeObject(message);
            bytes = Encoding.UTF8.GetBytes(json);
            var lengthBuffer = BitConverter.GetBytes((short)bytes.Length);
            await _writeStream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
            await _writeStream.WriteAsync(bytes, 0, bytes.Length);
            await _writeStream.FlushAsync();
        }

        public async Task Open()
        {
            if (!Directory.Exists(_queueDirectory))
                Directory.CreateDirectory(_queueDirectory);

            _fullPath = Path.Combine(_queueDirectory, _fileName);
            _writeStream = new FileStream(_fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _readStream = new FileStream(_fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            await _metaFile.Open();
            _readStream.Position = _metaFile.NextRecordPosition;
            await CountRecords();
            _readStream.Position = _metaFile.NextRecordPosition;
        }

        private async Task CountRecords()
        {
            while (true)
            {
                var len = _readStream.ReadByte();
                if (len == -1) break;

                var buffer = new byte[1000];
                var bytesRead = await _readStream.ReadAsync(buffer, 0, len);
                if (bytesRead != len)
                    throw new InvalidOperationException("Could not read a complete type name");

                var dataLengthBuffer = new byte[2];
                _readStream.Read(dataLengthBuffer, 0, dataLengthBuffer.Length);
                var dataLength = (int)BitConverter.ToUInt16(dataLengthBuffer, 0);

                _readStream.Position += dataLength;
                RecordCount++;
            }
        }
    }
}