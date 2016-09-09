﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Exchange
{
    public class MarketOrderRepository : IMarketOrderRepository
    {
        private static readonly Dictionary<string, List<MarketOrder>> _orders =
            new Dictionary<string, List<MarketOrder>>();

        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;

        public MarketOrderRepository(IAssetPairQuoteRepository assetPairQuoteRepository)
        {
            _assetPairQuoteRepository = assetPairQuoteRepository;
        }

        public async Task AddAsync(string accountId, string assetPairId, double volume)
        {
            var currentQuote = await _assetPairQuoteRepository.GetAsync(assetPairId);

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

            if (_orders.ContainsKey(accountId))
                _orders[accountId].Add(orderInfo);
            else
                _orders.Add(accountId, new List<MarketOrder> {orderInfo});
        }

        public Task<IEnumerable<MarketOrder>> GetAllAsync(string accountId)
        {
            if (_orders.Count == 0)
                return TaskEx.Null<IEnumerable<MarketOrder>>();

            IEnumerable<MarketOrder> accountOrders = _orders[accountId];

            return Task.FromResult(accountOrders);
        }

        public Task<MarketOrder> GetAsync(string accountId, string orderId)
        {
            if (_orders.Count == 0)
                throw new InvalidOperationException("no orders exist");

            if (!_orders.ContainsKey(accountId))
                throw new InvalidOperationException("account doesn't have any order");

            if (_orders[accountId].All(o => o.Id != orderId))
                throw new InvalidOperationException("invalid order");

            return Task.FromResult(_orders[accountId].FirstOrDefault(o => o.Id == orderId));
        }

        public async Task DeleteAsync(string accountId, string orderId)
        {
            var order = await GetAsync(accountId, orderId);

            _orders[accountId].Remove(order);

            if (_orders[accountId] == null)
                _orders.Remove(accountId);
        }
    }
}