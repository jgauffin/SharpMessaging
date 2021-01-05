using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SharpMessaging.Core;
using SharpMessaging.Core.Persistence.Disk;

namespace SharpMessaging.LabApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var queue = new FileQueue(@"C:\Temp\Queues", "Mine");
            await queue.Open();
            await queue.Enqueue(new MyMessage("go go 2"));

            var containerBuilder = new ServiceCollection();
            containerBuilder.AddScoped<IMessageHandler<MyMessage>, MyMessageHandler>();
            var container = containerBuilder.BuildServiceProvider();

            var service = new MessagingService(container, queue);
            var config = new MessagingConfiguration
            {
                QueueName = "Mine",
                QueueDirectory = @"C:\Temp\Queues"
            };
            await service.Run(config, CancellationToken.None);


            //var message = await queue.Dequeue();

            Console.WriteLine("Hello World!");
        }
    }
}