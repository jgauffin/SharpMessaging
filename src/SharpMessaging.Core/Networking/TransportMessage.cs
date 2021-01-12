using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMessaging.Core.Networking
{
    public class TransportMessage
    {
        public TransportMessage(object body)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public IDictionary<string,string> Headers { get; set; }
        public object Body { get; set; }
    }
}
