using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Persistence.Disk;

namespace SharpMessaging.Core
{
    public class MessagingClient
    {
        private readonly MessagingClientConfiguration _configuration;
        private readonly FileQueue _fileQueue;
        private readonly TcpMessagingClient _tcpClient;

        public MessagingClient(MessagingClientConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _fileQueue = new FileQueue(_configuration.QueueDirectory, _configuration.EndpointName);
            _tcpClient = new TcpMessagingClient();
        }

        private async Task DeliverQueuedMessages()
        {
            while (true)
            {
                try
                {
                    await _tcpClient.EnsureConnected();
                }
                catch (SocketException)
                {
                    await Task.Delay(5000);
                    continue;
                }


                var msg = await _fileQueue.Dequeue(TimeSpan.FromSeconds(10));
                if (msg == null) continue;

                try
                {
                    await _tcpClient.SendAsync(msg.Message);
                    await msg.Complete();
                }
                catch
                {
                    await msg.Abort();
                    throw;
                }
            }
        }

        public async Task Send(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            await _fileQueue.Enqueue(message);
        }

        public async Task Start()
        {
            await _fileQueue.Open();
            await _tcpClient.Open(_configuration.RemoteEndPointHostName, 8335);
#pragma warning disable 4014
            DeliverQueuedMessages();
#pragma warning restore 4014
        }
    }
}