using System.IO;
using System.Threading.Tasks;

namespace MatchingEngine.AzureStorage
{
    public interface IBlobStorage
    {
        Task<Stream> GetAsync(string blobContainer, string key);
    }
}