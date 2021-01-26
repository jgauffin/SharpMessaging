using System;
using System.Collections.Generic;

namespace SharpMessaging.Core.Networking.Messages
{
    public class TransportMessage
    {
        public TransportMessage(object body)
        {
            Id = Guid.NewGuid();
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public object Body { get; set; }
        public Guid CorrelationId { get; set; }

        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public Guid Id { get; set; }
    }
}