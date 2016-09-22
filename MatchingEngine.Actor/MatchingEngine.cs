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
using MatchingEngine.BusinessService.Events;
using MatchingEngine.BusinessService.Exchange;
using MatchingEngine.BusinessService.Proxy;
using MatchingEngine.Utils.Extensions;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngine.Actor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class MatchingEngine : Microsoft.ServiceFabric.Actors.Runtime.Actor, IMatchingEngine
    {
        private readonly IAccountInfoRepository _accountInfoRepository;
        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;
        private readonly IDictionaryProxy _dictionaryProxy;
        private readonly IMarketOrderRepository _marketOrderRepository;
        private readonly IMatchingEngineEventSubscriber _matchingEngineEventSubscriber;
        private readonly IOrderBookService _orderBookService;
        private readonly IOrderCalculator _orderCalculator;
        private readonly IPendingOrderRepository _pendingOrderRepository;
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;
        private IActorTimer _updateAssetTimer;

        public MatchingEngine(IDictionaryProxy dictionaryProxy,
            IAccountInfoRepository accountInfoRepository, IAssetPairQuoteRepository assetPairQuoteRepository,
            IMarketOrderRepository marketOrderRepository, IPendingOrderRepository pendingOrderRepository,
            ITransactionHistoryRepository transactionHistoryRepository, IOrderCalculator orderCalculator,
            IMatchingEngineEventSubscriber matchingEngineEventSubscriber, IOrderBookService orderBookService)
        {
            _dictionaryProxy = dictionaryProxy;
            _accountInfoRepository = accountInfoRepository;
            _assetPairQuoteRepository = assetPairQuoteRepository;
            _marketOrderRepository = marketOrderRepository;
            _pendingOrderRepository = pendingOrderRepository;
            _transactionHistoryRepository = transactionHistoryRepository;
            _orderCalculator = orderCalculator;
            _matchingEngineEventSubscriber = matchingEngineEventSubscriber;
            _orderBookService = orderBookService;
        }

        public Task InitAsync()
        {
            return TaskEx.Empty;
        }

        public async Task SubscribeAsync(string subscriberName)
        {
            await _matchingEngineEventSubscriber.SubscribeAsync(subscriberName);
        }

        public Task<AccountInfo> GetAccountInfoAsync(string accountId)
        {
            return _accountInfoRepository.GetByIdAsync(accountId);
        }

        public async Task<OrderInfo> OpenOrderAsync(string accountId, string assetPairId, double volume, double definedPrice)
        {
            if (!double.IsNaN(definedPrice))
            {
                var orderInfo = new PendingOrder
                {
                    ClientId = accountId,
                    AssetPairId = assetPairId,
                    Volume = volume,
                    Id = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    DefinedPrice = definedPrice
                };

                await _pendingOrderRepository.AddAsync(orderInfo);

                return Mapper.Map<OrderInfo>(orderInfo);
            }
            else
            {
                var currentQuote = await _assetPairQuoteRepository.GetByIdAsync(assetPairId);

                if (currentQuote == null)
                    throw new InvalidOperationException();

                var orderInfo = new MarketOrder
                {
                    ClientId = accountId,
                    AssetPairId = assetPairId,
                    Volume = volume,
                    Id = Guid.NewGuid().ToString(),
                    CreatedAt = currentQuote.DateTime
                };

                orderInfo.Price = orderInfo.OrderAction == OrderAction.Buy ? currentQuote.Ask : currentQuote.Bid;

                await _marketOrderRepository.AddAsync(orderInfo);

                return Mapper.Map<OrderInfo>(orderInfo);
            }
        }

        public async Task CloseOrderAsync(string accountId, string orderId)
        {
            var account = await _accountInfoRepository.GetByIdAsync(accountId);

            var activeOrder = await _marketOrderRepository.GetAsync(accountId, orderId);

            var assetPair = await _dictionaryProxy.GetAssetPairByIdAsync(activeOrder.AssetPairId);

            var quote = await _assetPairQuoteRepository.GetByIdAsync(activeOrder.AssetPairId);

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

            await _matchingEngineEventSubscriber.AccountUpdatedAsync(accountId);
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

        public async Task<IEnumerable<OrderBook>> GetOrderBookAsync()
        {
            return await _orderBookService.BuildOrderBookAsync();
        }

        public async Task<IEnumerable<TransactionHistory>> GetTransactionsHistoryAsync(string accountId)
        {
            return await _transactionHistoryRepository.GetAllAsync(accountId);
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

                await _assetPairQuoteRepository.UpdateAsync(assetPairQuote);
                updatedQuote = await _assetPairQuoteRepository.GetByIdAsync(assetPairQuote.AssetPairId);
            }
            else
            {
                updatedQuote = await _assetPairQuoteRepository.UpdateAsync(assetPair);
            }

            await _matchingEngineEventSubscriber.AssetPairPriceUpdatedAsync(updatedQuote);

            if (pendingOrders != null)
            {
                await UpdatePendingOrdersAsync(updatedQuote);
            }
        }

        private async Task UpdatePendingOrdersAsync(AssetPairQuote updatedQuote)
        {
            var pendingOrders =
                (await _pendingOrderRepository.FindByAssetPairIdAsync(updatedQuote.AssetPairId)).ToList();

            foreach (var pendingOrder in pendingOrders)
                if (((pendingOrder.OrderAction == OrderAction.Buy) && (pendingOrder.DefinedPrice >= updatedQuote.Bid)) ||
                    ((pendingOrder.OrderAction == OrderAction.Sell) && (pendingOrder.DefinedPrice <= updatedQuote.Ask)))
                {
                    var currentQuote = await _assetPairQuoteRepository.GetByIdAsync(updatedQuote.AssetPairId);

                    if (currentQuote == null)
                        throw new InvalidOperationException();

                    var orderInfo = new MarketOrder
                    {
                        ClientId = pendingOrder.ClientId,
                        AssetPairId = pendingOrder.AssetPairId,
                        Volume = pendingOrder.Volume,
                        Id = Guid.NewGuid().ToString(),
                        CreatedAt = currentQuote.DateTime
                    };

                    orderInfo.Price = orderInfo.OrderAction == OrderAction.Buy ? currentQuote.Ask : currentQuote.Bid;

                    await
                        _marketOrderRepository.AddAsync(orderInfo);

                    await _pendingOrderRepository.DeleteAsync(pendingOrder.ClientId, pendingOrder.Id);

                    await _matchingEngineEventSubscriber.ActiveOrdersUpdatedAsync(pendingOrder.ClientId);
                }
        }
    }
}