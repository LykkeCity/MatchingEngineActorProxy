using System;
using System.Threading.Tasks;
using Common.Extenstions;
using Core.Domain.Assets.Models;
using Core.Domain.Dictionary;
using Core.Domain.MatchingEngine;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngineActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class MatchingEngine : Actor, IMatchingEngine
    {
        private double _balance;
        private IActorTimer updateTimer;

        public Task InitAsync()
        {
            return TaskEx.Empty;
        }

        public Task<string> HandleMarketOrderAsync(string clientId, string assetPairId, OrderAction orderAction,
            double volume, bool straight)
        {
            throw new NotImplementedException();
        }

        public Task HandleLimitOrderAsync(string clientId, string assetPairId, OrderAction orderAction, double volume,
            double price)
        {
            throw new NotImplementedException();
        }

        public Task<CashInOutResponse> CashInOutBalanceAsync(string clientId, string assetId, double balanceDelta,
            bool sendToBlockchain,
            string correlationId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateBalanceAsync(string clientId, string assetId, double value)
        {
            throw new NotImplementedException();
        }

        public Task CancelLimitOrderAsync(int orderId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateWalletCredsForClient(string clientId)
        {
            throw new NotImplementedException();
        }

        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            updateTimer = RegisterTimer(UpdateNumber, null, TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(3));

            return TaskEx.Empty;
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