using System.Collections.Generic;

namespace SharpMessaging.Core.Networking
{
    public interface ITransportSerializer
    {
        object Deserialize(byte[] buffer, int offset, int length, IDictionary<string, string> headers);
        byte[] Serialize(object message, IDictionary<string, string> headers);
    }
}