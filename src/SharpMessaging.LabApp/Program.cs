using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SharpMessaging.Core;
using SharpMessaging.Core.Networking;
using SharpMessaging.Core.Persistence.Disk;

namespace SharpMessaging.LabApp
{
    internal class Program
    {
        private static async Task Main()
        {
            var queue = new QueueFile(@"C:\Temp\Queues", "Mine");
            await queue.Open();
            await queue.Enqueue(new MyMessage("go go 2"));


            var tasks = new[]
            {
                Application1(),
                Application2()
            };
            await Task.WhenAll(tasks);


            //var message = await queue.Dequeue();

            Console.WriteLine("Hello World!");
        }

        public static async Task Application1()
        {
            var config = new MessagingClientConfiguration
            {
                EndpointName = "MySampleApp",
                QueueDirectory = @"C:\Temp\Queues",
                RemoteEndPointHostName = "localhost"
            };

            var client = new MessagingClient(config);
            await client.Start();
            await client.Send(new MyMessage("DoIt"));
        }

        public static async Task Application2()
        {
            var containerBuilder = new ServiceCollection();
            containerBuilder.AddScoped<IMessageHandler<MyMessage>, MyMessageHandler>();
            var container = containerBuilder.BuildServiceProvider();

            var service = new MessagingService(container);
            var config = new MessagingConfiguration
            {
                QueueName = "Mine",
                QueueDirectory = @"C:\Temp\Queues",
                ListenerPort = 8335
            };
            await service.Run(config, CancellationToken.None);
        }
    }
}