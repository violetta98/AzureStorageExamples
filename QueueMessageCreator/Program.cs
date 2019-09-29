using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace QueueMessageCreator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Task.Run(() => ProcessAsync());
        }

        private static async Task ProcessAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            var storageAccount = CloudStorageAccount.Parse(configuration.GetConnectionString("StorageConnectionString"));

            var cloudQueueClient = storageAccount.CreateCloudQueueClient();

            var queue = cloudQueueClient.GetQueueReference("examplequeue");

            await queue.CreateIfNotExistsAsync();

            // adding a message to the queue

            for (var i = 0; i < 10; i++)
            {
                var message = new CloudQueueMessage($"Message {i}");
                await queue.AddMessageAsync(message);
            }

            Console.WriteLine("Messages were added to the queue");
            Console.ReadLine();
        }
    }
}
