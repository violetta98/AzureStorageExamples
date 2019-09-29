using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace BlobExample
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

            var blobClient = storageAccount.CreateCloudBlobClient();

            var blobContainer = blobClient.GetContainerReference("exampleblob");

            await blobContainer.CreateIfNotExistsAsync();

            await blobContainer.SetPermissionsAsync(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });

            var directoryConfig = configuration.GetSection("DirectoryConfig").Get<DirectoryConfig>();

            List<string> files;

            // we try to get a list of files in this directory
            try
            {
                files = Directory.EnumerateFiles(directoryConfig.Path1).ToList();

                foreach(var file in files)
                {
                    Console.WriteLine(file);
                }

                Console.WriteLine();
                Console.WriteLine($"{files.Count} were found in directory: {directoryConfig.Path1}");
            }
            catch(Exception e)
            {
                Console.WriteLine($"Couldn't retrieve files from directory: {directoryConfig.Path1}");
                Console.WriteLine($"Reason: {e.Message}");

                return;
            }

            Console.WriteLine("Are you sure that you want to upload these files to Azure Blob Storage? (y/n)");
            var result = Console.ReadLine();

            if (result?.ToUpper() != "Y")
            {
                return;
            }

            await UploadFilesToBlobStorageAsync(files, blobContainer);
            
            Console.WriteLine();
            Console.WriteLine($"Next we will download files to directory: {directoryConfig.Path2}. Press on enter.");
            Console.ReadLine();

            await DownloadFilesFromBlobStorageAsync(blobContainer, directoryConfig);

            var blobCount = blobContainer.ListBlobs().Count();

            Console.WriteLine();
            Console.WriteLine($"There are {blobCount} remaining in container");
            Console.WriteLine("Finished");
        }

        private static async Task UploadFilesToBlobStorageAsync(List<string> files, CloudBlobContainer blobContainer)
        {
            foreach (var file in files)
            {
                var fs = File.OpenRead(file); // read the file content into a FileStream buffer
                var fileName = Path.GetFileName(file);

                var blockBlob = blobContainer.GetBlockBlobReference(fileName);
                await blockBlob.UploadFromStreamAsync(fs);

                Console.WriteLine($"{file} was uploaded successfully...");
            }
        }

        private static async Task DownloadFilesFromBlobStorageAsync(CloudBlobContainer blobContainer, DirectoryConfig directoryConfig)
        {
            var listOfBlobs = blobContainer.ListBlobs();

            foreach (var blobItem in listOfBlobs)
            {
                var blob = blobItem as CloudBlockBlob;
                if (blob == null)
                {
                    continue;
                }

                Console.WriteLine($"Block blob of length: {blob.Properties.Length}, uri: {blob.Uri}");

                var blockBlob = blobContainer.GetBlockBlobReference(blob.Name);

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blob.Name);
                var fileExtension = Path.GetExtension(blob.Name);

                var pathToSave = $"{directoryConfig.Path2}\\{fileNameWithoutExtension}-Downloaded{fileExtension}";

                using (var fileStream = File.OpenWrite(pathToSave))
                {
                    await blockBlob.DownloadToStreamAsync(fileStream);
                }

                await blockBlob.DeleteAsync();
            }
        }
    }
}
