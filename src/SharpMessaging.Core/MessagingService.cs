using System;
using System.Threading;
using System.Threading.Tasks;
using SharpMessaging.Core.Persistence.Disk;

namespace SharpMessaging.Core
{
    public class MessagingService
    {
        private FileQueue _fileQueue;
        private readonly IServiceProvider _serviceProvider;

        public MessagingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public MessagingService(IServiceProvider serviceProvider, FileQueue fileQueue)
        {
            _serviceProvider = serviceProvider;
            _fileQueue = fileQueue;
        }

        public async Task Run(MessagingConfiguration config, CancellationToken token)
        {
            if (_fileQueue == null)
            {
                _fileQueue = new FileQueue(config.QueueDirectory, config.QueueName);
                await _fileQueue.Open();
            }

            while (true)
            {
                var messageToProcess = await _fileQueue.Dequeue();
                if (messageToProcess == null)
                {
                    await Task.Delay(100, token);
                    if (token.IsCancellationRequested)
                        return;
                    continue;
                }

                var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageToProcess.GetType());
                var instance = _serviceProvider.GetService(handlerType);
                if (instance == null)
                    continue;

                var method = instance.GetType().GetMethod("Handle",
                    new[] {typeof(MessageHandlerContext), messageToProcess.GetType()});
                if (method == null)
                    throw new InvalidOperationException(
                        $"Opps, did not find method 'Handle' in type '{instance.GetType()}'.");

                var context = new MessageHandlerContext();
                method.Invoke(instance, new[] {context, messageToProcess});
            }
        }
    }

    public class MessagingConfiguration
    {
        public string QueueDirectory { get; set; }
        public string QueueName { get; set; }
    }
}