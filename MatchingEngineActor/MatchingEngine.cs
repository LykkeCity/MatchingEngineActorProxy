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
        private readonly IDictionaryService _dictionaryProxy;

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

        }

        private Task UpdateNumber(object obj)
        {
            var rnd = new Random();

            _balance = rnd.NextDouble()*100;
            var ev = GetEvent<IMatchingEngineEvents>();
            ev.BalanceUpdated(_balance);

            return TaskEx.Empty;
        }
    }
}