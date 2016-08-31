using System;
using System.Collections.Generic;
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

        public DictionaryProxy()
        {
            var matchingEngineServiceUri = new Uri("fabric:/DictionaryApp/DictionaryServiceActorService");
            var actorId = ActorId.CreateRandom();
            _actorProxy = ActorProxy.Create<IDictionaryService>(actorId, matchingEngineServiceUri);
        }

        public async Task<IEnumerable<AssetPair>> GetAssetPairsAsync()
        {
            return await _actorProxy.GetAssetPairsAsync();
        }
    }
}