namespace SharpMessaging.Core.Networking
{
    public enum TransportMessageType : byte
    {
        Message = 1,
        Ack = 2,
        Nak = 3
    }
}