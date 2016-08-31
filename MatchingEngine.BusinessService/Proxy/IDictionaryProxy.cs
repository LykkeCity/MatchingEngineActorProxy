using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets.Models;

namespace MatchingEngine.BusinessService.Proxy
{
    public interface IDictionaryProxy : IProxy
    {
        Task<IEnumerable<AssetPair>> GetAssetPairsAsync();

        Task<AssetPair> GetAssetPairAsync(string baseAssetId, string quotingAssetId);
    }
}