using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking.Helpers;

namespace SharpMessaging.Core.Networking
{
    public static class SocketExtensions
    {
        public static async Task SendAsync(this Socket socket, SocketAwaitable awaitable)
        {
            var isPending = socket.SendAsync(awaitable._eventArgs);
            if (!isPending)
                return;

            awaitable.Reset();
            await awaitable;
        }
    }
}
