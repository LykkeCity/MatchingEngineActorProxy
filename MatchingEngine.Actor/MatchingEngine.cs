using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Account;
using Lykke.Core.Domain.Account.Models;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Assets.Models;
using Lykke.Core.Domain.Dictionary;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using Lykke.Core.Domain.MatchingEngine;
using MatchingEngine.Actor.Proxy;
using MatchingEngine.DataAccess.Account;
using MatchingEngine.DataAccess.Asset;
using MatchingEngine.DataAccess.Exchange;
using MatchingEngine.Utils.Extensions;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngine.Actor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class MatchingEngine : Microsoft.ServiceFabric.Actors.Runtime.Actor, IMatchingEngine
    {
        private readonly IAccountInfoRepository _accountInfoRepository;
        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;
        private readonly IOrderInfoRepository _orderInfoRepository;
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;
        private readonly IDictionaryService _dictionaryProxy;
        private StatefulServiceContext _context;
        private IActorTimer _updateAssetTimer;

        public MatchingEngine(StatefulServiceContext context)
        {
            _context = context;
            _dictionaryProxy = new DictionaryProxy().Connect(context);

            //TODO: refactor
            _accountInfoRepository = new AccountInfoRepository();
            _assetPairQuoteRepository = new AssetPairQuoteRepository();
            _orderInfoRepository = new OrderInfoRepository(_assetPairQuoteRepository);
            _transactionHistoryRepository = new TransactionHistoryRepository();
        }

        public Task InitAsync()
        {
            return TaskEx.Empty;
        }

        public Task<AccountInfo> GetAccountInfoAsync(string accountId)
        {
            return _accountInfoRepository.GetAsync(accountId);
        }

        public async Task OpenOrderAsync(string accountId, string assetPairId, double volume)
        {
            await _orderInfoRepository.AddAsync(accountId, assetPairId, volume);
        }

        public async Task CloseOrderAsync(string accountId, string orderId)
        {
            var activeOrder = await _orderInfoRepository.GetAsync(accountId, orderId);

            var quote = await _assetPairQuoteRepository.GetAsync(activeOrder.AssetPairId);

            await _transactionHistoryRepository.AddAsync(activeOrder, quote);

            await _orderInfoRepository.DeleteAsync(accountId, orderId);
        }

        public async Task<IEnumerable<OrderInfo>> GetActiveOrdersAsync(string accountId)
        {
            return await _orderInfoRepository.GetAllAsync(accountId);
        }

        public async Task<IEnumerable<AssetPairQuote>> GetMarketProfile()
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

            var rnd = new Random();

            var assetId = rnd.Next(assetPairs.Count);

            var assetPair = assetPairs[assetId];

            var updatedQuote = await _assetPairQuoteRepository.UpdateAsync(assetPair);

            var ev = GetEvent<IMatchingEngineEvents>();
            ev.AssetPairPriceUpdated(updatedQuote);
        }
    }
}