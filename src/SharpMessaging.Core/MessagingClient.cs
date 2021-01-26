using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Networking.Messages;
using SharpMessaging.Core.Persistence.Disk;

namespace SharpMessaging.Core
{
    public class MessagingClient
    {
        private readonly MessagingClientConfiguration _configuration;
        private readonly FileQueue _fileQueue;
        private readonly TcpMessagingClient _tcpClient;
        private byte[] _readBuffer = new byte[65535];
        private DequeuedMessage _pendingMessage;
        private Guid _pendingMessageId;


        public MessagingClient(MessagingClientConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _fileQueue = new FileQueue(_configuration.QueueDirectory, _configuration.EndpointName);
            _tcpClient = new TcpMessagingClient();
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
            _tcpClient.SetReceiver(ProcessReceivedMessage);
#pragma warning disable 4014
            DeliverQueuedMessages();
#pragma warning restore 4014
        }

        private async Task ProcessReceivedMessage(object message)
        {
            if (message is Ack ack)
            {
                if (_pendingMessageId != ack.MessageId)
                    throw new InvalidOperationException("We screwed up.");

                await _pendingMessage.Complete();
                _pendingMessage = null;
            }
            else if (message is Nak nak)
            {
                if (_pendingMessageId != nak.MessageId)
                    throw new InvalidOperationException("We screwed up.");

                await _pendingMessage.Abort();
                _pendingMessage = null;
            }
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


                _pendingMessage = await _fileQueue.Dequeue(TimeSpan.FromSeconds(10));
                if (_pendingMessage == null) continue;

                try
                {
                    var transportMessage = new TransportMessage(_pendingMessage.Message);
                    _pendingMessageId = transportMessage.Id;
                    await _tcpClient.SendAsync(transportMessage);
                    await Task.Delay(600000);
                }
                catch
                {
                    await _pendingMessage.Abort();
                    _pendingMessage = null;
                    throw;
                }
            }
        }
    }
}