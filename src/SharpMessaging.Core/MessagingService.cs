using System;
using System.Threading;
using System.Threading.Tasks;
using SharpMessaging.Core.Networking;

namespace SharpMessaging.Core
{
    public class MessagingService : IMessageHandlerInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private MessagingListener _messagingListener;

        public MessagingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleAsync(object messageToProcess)
        {
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageToProcess.GetType());
            var instance = _serviceProvider.GetService(handlerType);
            if (instance == null)
                return;

            var method = instance.GetType().GetMethod("Handle",
                new[] {typeof(MessageHandlerContext), messageToProcess.GetType()});
            if (method == null)
                throw new InvalidOperationException(
                    $"Opps, did not find method 'Handle' in type '{instance.GetType()}'.");

            var context = new MessageHandlerContext();
            var task = (Task)method.Invoke(instance, new[] {context, messageToProcess});
            await task;
        }

        //public MessagingService(IServiceProvider serviceProvider, QueueFile fileQueue)
        //{
        //    _serviceProvider = serviceProvider;
        //    _fileQueue = fileQueue;
        //}

        public async Task Run(MessagingConfiguration config, CancellationToken token)
        {
            _messagingListener = new MessagingListener(config.ListenerPort, this);
            await _messagingListener.Run(token);
        }
    }
}