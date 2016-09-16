using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Core.Domain.Account;
using Lykke.Core.Domain.Account.Models;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Assets.Models;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using Lykke.Core.Domain.MatchingEngine;
using MatchingEngine.Actor.Events;
using MatchingEngine.BusinessService.Exchange;
using MatchingEngine.BusinessService.Proxy;
using MatchingEngine.Utils.Extensions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngine.Actor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class MatchingEngine : Microsoft.ServiceFabric.Actors.Runtime.Actor, IEventSubscriber, IMatchingEngine
    {
        private readonly IAccountInfoRepository _accountInfoRepository;
        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;
        private readonly IDictionaryProxy _dictionaryProxy;
        private readonly IMarketOrderRepository _marketOrderRepository;
        private readonly IOrderCalculator _orderCalculator;
        private readonly IPendingOrderRepository _pendingOrderRepository;
        private readonly IEventSubscriber _subscriber;
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;
        private IActorTimer _updateAssetTimer;

        public MatchingEngine(IDictionaryProxy dictionaryProxy,
            IAccountInfoRepository accountInfoRepository, IAssetPairQuoteRepository assetPairQuoteRepository,
            IMarketOrderRepository marketOrderRepository, IPendingOrderRepository pendingOrderRepository,
            ITransactionHistoryRepository transactionHistoryRepository, IOrderCalculator orderCalculator,
            IEventSubscriber subscriber)
        {
            _dictionaryProxy = dictionaryProxy;
            _accountInfoRepository = accountInfoRepository;
            _assetPairQuoteRepository = assetPairQuoteRepository;
            _marketOrderRepository = marketOrderRepository;
            _pendingOrderRepository = pendingOrderRepository;
            _transactionHistoryRepository = transactionHistoryRepository;
            _orderCalculator = orderCalculator;
            _subscriber = subscriber;
        }

        public Task InitAsync()
        {
            return TaskEx.Empty;
        }

        public async Task<AccountInfo> GetAccountInfoAsync(string accountId)
        {
            return await _accountInfoRepository.GetAsync(accountId);
        }

        public async Task OpenOrderAsync(string accountId, string assetPairId, double volume, double definedPrice)
        {
            if (!double.IsNaN(definedPrice))
                await _pendingOrderRepository.AddAsync(accountId, assetPairId, volume, definedPrice);
            else
                await _marketOrderRepository.AddAsync(accountId, assetPairId, volume);
        }

        public async Task CloseOrderAsync(string accountId, string orderId)
        {
            var account = await _accountInfoRepository.GetAsync(accountId);

            var activeOrder = await _marketOrderRepository.GetAsync(accountId, orderId);

            var assetPair = await _dictionaryProxy.GetAssetPairByIdAsync(activeOrder.AssetPairId);

            var quote = await _assetPairQuoteRepository.GetAsync(activeOrder.AssetPairId);

            var transactionHistory = new TransactionHistory
            {
                AccountId = activeOrder.ClientId,
                AssetPairId = activeOrder.AssetPairId,
                CompletedAt = DateTime.UtcNow,
                TransactionId = activeOrder.Id,
                Price = activeOrder.OrderAction == OrderAction.Buy ? quote.Ask : quote.Bid
            };

            var profitLoss =
                await _orderCalculator.CalculateProfitLossAsync(activeOrder.Price, transactionHistory.Price,
                    activeOrder.Volume, assetPair, account.BaseAssetId);

            transactionHistory.ProfitLoss = profitLoss;

            await _transactionHistoryRepository.AddAsync(transactionHistory);

            await _marketOrderRepository.DeleteAsync(accountId, orderId);

            account.Balance += profitLoss;
            await _accountInfoRepository.UpdateAsync(account);

            //todo: move this to separate method if it's possible to pass interface method in parameters
            foreach (var subscribers in GetActiveSubsribers())
                try
                {
                    await subscribers.Value.OnAccountUpdated(accountId);
                }
                catch (Exception)
                {
                    _subscriber.Unsubscribe(subscribers);
                }
        }

        public async Task<IEnumerable<OrderInfo>> GetActiveOrdersAsync(string accountId)
        {
            var marketOrder = await _marketOrderRepository.GetAllAsync(accountId) ?? new List<MarketOrder>();
            var pendingOrders = await _pendingOrderRepository.GetAllAsync(accountId) ?? new List<PendingOrder>();
            var orderInfo =
                marketOrder.Select(Mapper.Map<OrderInfo>).Union<OrderInfo>(pendingOrders.Select(Mapper.Map<OrderInfo>));

            return orderInfo;
        }

        public async Task<IEnumerable<AssetPairQuote>> GetMarketProfileAsync()
        {
            return await _assetPairQuoteRepository.GetAllAsync();
        }

        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            var assetPairs = await _dictionaryProxy.GetAssetPairsAsync();
            await _assetPairQuoteRepository.AddAllAsync(assetPairs);

            await StateManager.TryAddStateAsync("AssetPairs", assetPairs);

            _updateAssetTimer = RegisterTimer(UpdateAssetPairAsync, null, TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10));
        }

        private async Task UpdateAssetPairAsync(object obj)
        {
            var assetPairs = (await StateManager.GetStateAsync<IEnumerable<AssetPair>>("AssetPairs")).ToList();

            var pendingOrders = await _pendingOrderRepository.GetAllAsync(null);

            var rnd = new Random();

            var assetId = rnd.Next(assetPairs.Count);

            var assetPair = assetPairs[assetId];

            AssetPairQuote updatedQuote;

            if (pendingOrders != null)
            {
                var orders = pendingOrders as IList<PendingOrder> ?? pendingOrders.ToList();
                var assetPairIds = orders.Select(p => p.AssetPairId).Distinct().ToList();
                assetPairIds.Add(assetPair.Id);

                assetId = rnd.Next(assetPairIds.Count);

                var pendingOrder = orders.First(p => p.AssetPairId.Equals(assetPairIds[assetId]));
                var assetPairQuote = new AssetPairQuote
                {
                    AssetPairId = pendingOrder.AssetPairId
                };
                if (pendingOrder.OrderAction == OrderAction.Buy)
                {
                    assetPairQuote.Bid = pendingOrder.DefinedPrice - rnd.NextDouble();
                    assetPairQuote.Ask = assetPairQuote.Bid + rnd.NextDouble();
                }
                else
                {
                    assetPairQuote.Ask = pendingOrder.DefinedPrice + rnd.NextDouble();
                    assetPairQuote.Bid = assetPairQuote.Bid - rnd.NextDouble();
                }

                updatedQuote = await _assetPairQuoteRepository.UpdateAsync(assetPairQuote);
            }
            else
            {
                updatedQuote = await _assetPairQuoteRepository.UpdateAsync(assetPair);
            }

            foreach (var subscribers in GetActiveSubsribers())
                try
                {
                    await subscribers.Value.OnAssetPairPriceUpdated(updatedQuote);
                }
                catch (Exception)
                {
                    _subscriber.Unsubscribe(subscribers);
                }

            if (pendingOrders != null)
                await UpdatePendingOrdersAsync(updatedQuote);
        }

        private async Task UpdatePendingOrdersAsync(AssetPairQuote updatedQuote)
        {
            var pendingOrders =
                (await _pendingOrderRepository.FindByAssetPairIdAsync(updatedQuote.AssetPairId)).ToList();

            foreach (var pendingOrder in pendingOrders)
                if (((pendingOrder.OrderAction == OrderAction.Buy) && (pendingOrder.DefinedPrice >= updatedQuote.Bid)) ||
                    ((pendingOrder.OrderAction == OrderAction.Sell) && (pendingOrder.DefinedPrice <= updatedQuote.Ask)))
                {
                    await
                        _marketOrderRepository.AddAsync(pendingOrder.ClientId, pendingOrder.AssetPairId,
                            pendingOrder.Volume);

                    await _pendingOrderRepository.DeleteAsync(pendingOrder.ClientId, pendingOrder.Id);

                    foreach (var subscribers in GetActiveSubsribers())
                        try
                        {
                            await subscribers.Value.OnActiveOrdersUpdated(pendingOrder.ClientId);
                        }
                        catch (Exception)
                        {
                            _subscriber.Unsubscribe(subscribers);
                        }
                }
        }

        #region IEventSubscriber implementation

        public async Task SubscribeAsync(IMatchingEngineEventSubscriber events)
        {
            await _subscriber.SubscribeAsync(events);
        }

        public Dictionary<ActorId, IMatchingEngineEventSubscriber> GetActiveSubsribers()
        {
            return _subscriber.GetActiveSubsribers();
        }

        public void Unsubscribe(KeyValuePair<ActorId, IMatchingEngineEventSubscriber> subscriber)
        {
            _subscriber.Unsubscribe(subscriber);
        }

        #endregion
    }
}