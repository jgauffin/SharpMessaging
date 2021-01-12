using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Networking
{
    public class MessagingListener
    {
        private readonly int _listenerPort;
        private TcpListener _tcpListener;
        LinkedList<MessagingListenerClient> _clients = new LinkedList<MessagingListenerClient>();
        private IMessageHandlerInvoker _messageHandlerInvoker;

        public MessagingListener(int listenerPort, IMessageHandlerInvoker messageHandlerInvoker)
        {
            if (listenerPort < 0) throw new ArgumentOutOfRangeException(nameof(listenerPort));
            _listenerPort = listenerPort;
            _messageHandlerInvoker = messageHandlerInvoker ?? throw new ArgumentNullException(nameof(messageHandlerInvoker));
            _tcpListener = new TcpListener(IPAddress.Any, listenerPort);
        }

        public async Task Run(CancellationToken token)
        {
            _tcpListener.Start();
            while (!token.IsCancellationRequested)
            {
                var socket = await _tcpListener.AcceptSocketAsync();
                var newClient = new MessagingListenerClient(socket, _messageHandlerInvoker);
#pragma warning disable 4014
                newClient.Run(token).ContinueWith(x=> OnClientDisconnected(x, newClient));
#pragma warning restore 4014

                lock (_clients)
                {
                    _clients.AddLast(newClient);
                }
            }
            
        }

        private void OnClientDisconnected(Task obj, MessagingListenerClient client)
        {
            lock (_clients)
            {
                _clients.Remove(client);
            }
        }
    }
}
