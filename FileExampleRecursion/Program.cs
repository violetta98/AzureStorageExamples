using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace FileExample
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

            var fileClient = storageAccount.CreateCloudFileClient();

            var fileShare = fileClient.GetShareReference("fs1");

            await fileShare.CreateIfNotExistsAsync();

            var directoryConfig = configuration.GetSection("DirectoryConfig").Get<DirectoryConfig>();

            if (!Directory.Exists(directoryConfig.Path))
            {
                Directory.CreateDirectory(directoryConfig.Path);
            }

            var rootDirectory = fileShare.GetRootDirectoryReference();

            await DownloadAllInDirectoryAsync(rootDirectory, directoryConfig.Path);
        }

        private static async Task DownloadAllInDirectoryAsync(CloudFileDirectory cloudDirectory, string path)
        {

            var filesAndDirectories = cloudDirectory.ListFilesAndDirectories();

            foreach (var listFileItem in filesAndDirectories)
            {
                await DownloadFile(listFileItem, cloudDirectory, path);
                await DownloadDirectory(listFileItem, cloudDirectory, path);
            }
        }

        private static async Task DownloadFile(IListFileItem listFileItem, CloudFileDirectory cloudDirectory, string path)
        {
            var cloudFile = listFileItem as CloudFile;

            if (cloudFile == null)
            {
                return;
            }

            cloudFile = cloudDirectory.GetFileReference(cloudFile.Name);

            var pathToSave = $"{path}\\{Path.GetFileName(cloudFile.Name)}";

            using (var fileStream = File.OpenWrite(pathToSave))
            {
                await cloudFile.DownloadToStreamAsync(fileStream);
            }
        }

        private static async Task DownloadDirectory(IListFileItem listFileItem, CloudFileDirectory cloudDirectory, string path)
        {
            var subDirectory = listFileItem as CloudFileDirectory;
            if (subDirectory == null)
            {
                return;
            }

            subDirectory = cloudDirectory.GetDirectoryReference(subDirectory.Name);
            var pathToSave = $"{path}\\{subDirectory.Name}";

            if (!Directory.Exists(pathToSave))
            {
                Directory.CreateDirectory(pathToSave);
            }

            await DownloadAllInDirectoryAsync(subDirectory, pathToSave);
        }
    }
}
