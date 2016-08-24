using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Common.Extenstions;
using Core.Domain.Assets.Models;
using Core.Domain.Dictionary;
using Core.Domain.Feed;
using Core.Domain.MatchingEngine;
using MatchingEngineActor.Proxy;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngineActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class MatchingEngine : Actor, IMatchingEngine
    {
        private StatefulServiceContext _context;
        private double _balance;
        private IActorTimer _updateTimer;
        private IActorTimer _updateAssetTimer;
        private readonly IDictionaryService _dictionaryProxy;
        private const double _bidPrice = 1.3253412;
        private const double _askPrice = 1.3262112;

        public MatchingEngine(StatefulServiceContext context)
        {
            this._context = context;
            _dictionaryProxy = new DictionaryProxy().Connect(context);
        }

        public Task InitAsync()
        {
            return TaskEx.Empty;
        }

        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            var assetPairs = await _dictionaryProxy.GetAssetPairsAsync();

            await this.StateManager.TryAddStateAsync("AssetPairs", assetPairs);

            _updateTimer = RegisterTimer(UpdateNumber, null, TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(3));

            _updateAssetTimer = RegisterTimer(UpdateAssetPair, null, TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2));
        }

        private Task UpdateNumber(object obj)
        {
            var rnd = new Random();

            _balance = rnd.NextDouble()*100;
            var ev = GetEvent<IMatchingEngineEvents>();
            ev.BalanceUpdated(_balance);

            return TaskEx.Empty;
        }

        private async Task UpdateAssetPair(object obj)
        {
            var assetPairs = (await this.StateManager.GetStateAsync<IEnumerable<AssetPair>>("AssetPairs")).ToList();

            var rnd = new Random();

            var assetId = rnd.Next(assetPairs.Count);

            var assetPair = assetPairs[assetId];

            var significantDigit = Math.Pow(10, -assetPair.Accuracy);
            var sign = rnd.Next(0, 1)*2 - 1;

            var ask = Math.Round(_askPrice + (sign*significantDigit), assetPair.Accuracy);
            var bid = Math.Round(_bidPrice + (sign*significantDigit), assetPair.Accuracy);

            var feedData = new FeedData
            {
                Asset = assetPair.Name,
                DateTime = DateTime.Now,
                Ask = ask,
                Bid = bid
            };

            var ev = GetEvent<IMatchingEngineEvents>();
            ev.AssetPairUpdated(feedData);
        }
    }
}