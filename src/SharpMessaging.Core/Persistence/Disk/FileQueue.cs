using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharpMessaging.Core.Persistence.Disk
{
    public class FileQueue
    {
        private readonly string _queueDirectory;
        private readonly string _queueName;
        private readonly QueueMetaFile _metaFile;
        private FileStream _readStream;
        private FileStream _writeStream;

        public FileQueue(string queueDirectory, string queueName)
        {
            _queueDirectory = queueDirectory ?? throw new ArgumentNullException(nameof(queueDirectory));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _metaFile = new QueueMetaFile(queueDirectory, queueName);
        }

        public async Task<object> Dequeue()
        {
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

            await _metaFile.WriteNextRecordPosition((int)_readStream.Position);
            var json = Encoding.UTF8.GetString(jsonBuffer, 0, bytesRead);
            return JsonConvert.DeserializeObject(json, typeToDeserialize);
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

            var file = Path.Combine(_queueDirectory, _queueName + ".data");
            _writeStream = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _readStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            await _metaFile.Open();
            _readStream.Position = _metaFile.NextRecordPosition;
        }
    }
}