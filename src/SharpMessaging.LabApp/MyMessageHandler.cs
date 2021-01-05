using System;
using System.Threading.Tasks;
using SharpMessaging.Core;

namespace SharpMessaging.LabApp
{
    internal class MyMessageHandler : IMessageHandler<MyMessage>
    {
        public Task Handle(MessageHandlerContext context, MyMessage message)
        {
            Console.WriteLine("We got called!");
            return Task.CompletedTask;
        }
    }
}