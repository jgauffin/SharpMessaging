namespace SharpMessaging.Core
{
    public class MessagingConfiguration
    {
        public string QueueDirectory { get; set; }
        public string QueueName { get; set; }

        /// <summary>
        /// Port that the TCP server should accept new connections on.
        /// </summary>
        public int ListenerPort { get; set; }
    }
}