using System;

namespace SharpMessaging.Core.Networking.Messages
{
    public class Nak
    {
        public Nak(Guid messageId)
        {
            if (messageId == Guid.Empty)
                throw new ArgumentException("Must be specified", nameof(messageId));

            MessageId = messageId;
        }

        public Guid MessageId { get; private set; }
    }
}