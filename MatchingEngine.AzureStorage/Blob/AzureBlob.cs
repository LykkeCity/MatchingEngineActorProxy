using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MatchingEngine.AzureStorage.Blob
{
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly CloudBlobClient _blobClient;

        public AzureBlobStorage(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task<Stream> GetAsync(string blobContainer, string key)
        {
            var containerRef = _blobClient.GetContainerReference(blobContainer);

            var blockBlob = containerRef.GetBlockBlobReference(key);
            var result = await blockBlob.DownloadTextAsync();
            var byteArray = Encoding.UTF8.GetBytes(result);
            var ms = new MemoryStream(byteArray);
            return ms;
        }
    }
}