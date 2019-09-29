using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace QueueMessageReader
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

            if (queue == null)
            {
                Console.WriteLine("Please run 'Queue Message Creator' firstly");
                return;
            }

            await queue.FetchAttributesAsync(); // this method asks the Queue service to retrieve the queue attributes, including the message count.

            if (queue.ApproximateMessageCount != null) // ApproximateMessageCount property returns the last value retrieved by the FetchAttributes method, without calling the Queue service.
            {
                Console.WriteLine($"Approximate count for the queue is {queue.ApproximateMessageCount}");
            }

            var peekedMessage = await queue.PeekMessageAsync();

            while (peekedMessage != null)
            {
                Console.WriteLine();
                Console.WriteLine($"Contents of message: {peekedMessage.AsString}");

                Console.WriteLine("What would you like to do?");
                Console.WriteLine("1 - Process / Delete message");
                Console.WriteLine("2 - Modify / Re-queue message");
                Console.WriteLine("3 - Delay / Re-queue message");
                Console.WriteLine("4 - Exit");

                var result = Console.ReadLine();

                CloudQueueMessage message = null;

                switch (result)
                {
                    case "1": // delete message
                        message = await queue.GetMessageAsync();
                        Console.WriteLine($"Retrieved message with content '{message.AsString}'");

                        await queue.DeleteMessageAsync(message);
                        Console.WriteLine("Deleted message");

                        break;

                    case "2": // update message
                        message = await queue.GetMessageAsync();
                        Console.WriteLine($"Retrieved message with content '{message.AsString}'");

                        message.SetMessageContent2($"Updated contents: {message.AsString}", false);
                        await queue.UpdateMessageAsync(message, TimeSpan.FromSeconds(0.0), MessageUpdateFields.Visibility | MessageUpdateFields.Content);
                        Console.WriteLine("Modified / re-queued message");

                        break;

                    case "3": // ignore message and make it invisible for 10 seconds
                        message = await queue.GetMessageAsync(TimeSpan.FromSeconds(10.0), null, null);
                        Console.WriteLine("Ignored / made invisible for 10 seconds message");

                        break;

                    default:
                        Environment.Exit(0);

                        break;
                }

                peekedMessage = await queue.PeekMessageAsync();
            }
        }
    }
}
