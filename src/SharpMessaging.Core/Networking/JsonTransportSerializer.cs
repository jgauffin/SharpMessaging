using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SharpMessaging.Core.Networking
{
    public class JsonTransportSerializer : ITransportSerializer
    {
        private const string ContentTypeHeader = "Content-Type";
        private const string TypeNameHeader = "Type-Name";

        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };

        public byte[] Serialize(object message, IDictionary<string, string> headers)
        {
            var msgJson = JsonConvert.SerializeObject(message, _settings);
            headers[TypeNameHeader] = message.GetType().AssemblyQualifiedName;
            headers[ContentTypeHeader] = "application/json";
            return Encoding.UTF8.GetBytes(msgJson);
        }

        public object Deserialize(byte[] buffer, int offset, int length, IDictionary<string, string> headers)
        {
            var json = Encoding.UTF8.GetString(buffer, offset, length);
            if (!headers.TryGetValue(TypeNameHeader, out var typeStr))
                throw new JsonSerializationException($"Failed to find '{TypeNameHeader}' in the message headers.");

            var type = Type.GetType(typeStr);
            if (type == null)
                throw new InvalidOperationException(
                    $"Type '{typeStr}' could not be found. Are you using the same API assembly and the correct version?");

            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        public int Serialize(object message, IDictionary<string, string> headers, byte[] buffer, int offset,
            int maxCount)
        {
            var msgJson = JsonConvert.SerializeObject(message, _settings);
            headers[TypeNameHeader] = message.GetType().AssemblyQualifiedName;
            headers[ContentTypeHeader] = "application/json";

            var len = Encoding.UTF8.GetByteCount(msgJson);
            if (maxCount < len)
                throw new InvalidOperationException("Requires larger buffer: wanted size: " + maxCount);
            return Encoding.UTF8.GetBytes(msgJson, 0, msgJson.Length, buffer, offset);
        }
    }
}