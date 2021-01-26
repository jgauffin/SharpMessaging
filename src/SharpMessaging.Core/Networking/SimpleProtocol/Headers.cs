namespace SharpMessaging.Core.Networking.SimpleProtocol
{
    public static class Headers
    {
        public const string MessageId = "MessageId";

        /// <summary>
        /// Amount of bytes used for the content
        /// </summary>
        public const string ContentLength = "Content-Length";


        /// <summary>
        /// The type which is being transported over the protocol.
        /// </summary>
        public const string TypeName = "Type-Name";

        /// <summary>
        /// Type of serialization format.
        /// </summary>
        public const string ContenType = "Content-Type";
    }
}
