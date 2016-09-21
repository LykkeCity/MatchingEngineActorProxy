using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets.Models;
using Lykke.Core.Domain.Dictionary;
using MatchingEngine.Domain.Settings;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace MatchingEngine.BusinessService.Proxy
{
    public class DictionaryProxy : IDictionaryProxy
    {
        private static IDictionaryService _actorProxy;
        private static IEnumerable<AssetPair> _assetPairs = new List<AssetPair>();
        private readonly FactorySettings _settings;

        public DictionaryProxy(FactorySettings settings)
        {
            _settings = settings;
            var matchingEngineServiceUri = new Uri(_settings.DictionaryFactoryUri);
            var actorId = ActorId.CreateRandom();
            _actorProxy = ActorProxy.Create<IDictionaryService>(actorId, matchingEngineServiceUri);
        }

        public async Task<IEnumerable<AssetPair>> GetAssetPairsAsync()
        {
            return _assetPairs.Any() ? _assetPairs : _assetPairs = await _actorProxy.GetAssetPairsAsync();
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