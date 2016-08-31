using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets.Models;
using Lykke.Core.Domain.Dictionary;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace MatchingEngine.BusinessService.Proxy
{
    public class DictionaryProxy : IDictionaryProxy
    {
        private static IDictionaryService _actorProxy;
        private static IEnumerable<AssetPair> _assetPairs = new List<AssetPair>();

        public DictionaryProxy()
        {
            var matchingEngineServiceUri = new Uri("fabric:/DictionaryApp/DictionaryServiceActorService");
            var actorId = ActorId.CreateRandom();
            _actorProxy = ActorProxy.Create<IDictionaryService>(actorId, matchingEngineServiceUri);
        }

        public async Task<IEnumerable<AssetPair>> GetAssetPairsAsync()
        {
            return _assetPairs ?? (_assetPairs = await _actorProxy.GetAssetPairsAsync());
        }

        public async Task<AssetPair> GetAssetPairAsync(string baseAssetId, string quotingAssetId)
        {
            if (!_assetPairs.Any())
                await GetAssetPairsAsync();

            return
                _assetPairs.FirstOrDefault(a => (a.BaseAssetId == baseAssetId) && (a.QuotingAssetId == quotingAssetId));
        }

        public async Task<AssetPair> GetAssetPairByIdAsync(string assetPairId)
        {
            if (!_assetPairs.Any())
                await GetAssetPairsAsync();

            return _assetPairs.FirstOrDefault(a => a.Id == assetPairId);
        }
    }
}