using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Microsoft.Azure.Cosmos.Table;
using TableExample.Entities;

namespace TableExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            var storageAccount = CloudStorageAccount.Parse(configuration.GetConnectionString("StorageConnectionString"));

            var tableClient = storageAccount.CreateCloudTableClient();

            var table = tableClient.GetTableReference("Users");

            table.CreateIfNotExists();

            while(true)
            {
                Console.WriteLine("\nPlease input 0 to exit from this loop or press enter to input user data:");

                var input = Console.ReadLine();

                if (input == "0")
                {
                    break;
                }

                Console.WriteLine("Please enter user name, email and role in a separate line:");
                var name = Console.ReadLine();
                var email = Console.ReadLine();
                var role = Console.ReadLine();

                AddUser(table, new User(name, email, role));
            }

            GetAllUsers(table);


            //while(true)
            //{
            //    Console.WriteLine("Change order quantity to ?");

            //    var quantity = Convert.ToInt32(Console.ReadLine());

            //    entity["Quantity"] = EntityProperty.GeneratePropertyForInt(quantity);

            //    var updateOperation = TableOperation.Merge(entity);

            //    try
            //    {
            //        result = table.Execute(updateOperation);
            //    }
            //    catch(Exception e)
            //    {
            //        Console.WriteLine(e.Message);

            //        var retrieveOperation = TableOperation.Retrieve(partitionKey, rowKey);

            //        result = table.Execute(retrieveOperation);
            //    }

            //    entity = (DynamicTableEntity)result.Result;
            //}

        }

        static void AddUser(CloudTable table, User user)
        {
            var insert = TableOperation.Insert(user);
            table.Execute(insert);
        }

        static void GetAllUsers(CloudTable table)
        {
            //var query = new TableQuery<User>()
            //        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Admin"));

            Console.WriteLine("\nAll Users");
            Console.WriteLine("-------------------------------------------------------------------------");

            var users = table.ExecuteQuery(new TableQuery<User>());

            foreach (var user in users)
            {
                Console.WriteLine($"{user.Name}, {user.Email}, {user.Role}");
            }

            Console.WriteLine("-------------------------------------------------------------------------");

        }
    }
}
