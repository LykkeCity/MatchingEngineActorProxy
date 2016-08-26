﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Assets.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Asset
{
    public class AssetPairQuoteRepository : IAssetPairQuoteRepository
    {
        private static IEnumerable<AssetPairQuote> _orders = new List<AssetPairQuote>();
        private readonly Random rnd = new Random();

        public Task<IEnumerable<AssetPairQuote>> GetAllAsync()
        {
            return Task.FromResult(_orders);
        }

        public Task<AssetPairQuote> GetAsync(string assertPairId)
        {
            var quote = _orders.FirstOrDefault(x => x.AssetPairId == assertPairId);

            return Task.FromResult(quote);
        }

        public Task AddAllAsync(IEnumerable<AssetPair> assetPairs)
        {
            var assetPairQuotes = new List<AssetPairQuote>();

            foreach (var assetPair in assetPairs)
            {
                var ask = rnd.NextDouble()*(3 - 0.1) + 0.1;
                var bid = ask - rnd.NextDouble();

                var assetPairQuote = new AssetPairQuote
                {
                    AssetPairId = assetPair.Id,
                    Ask = ask,
                    Bid = bid,
                    DateTime = DateTime.UtcNow
                };

                assetPairQuotes.Add(assetPairQuote);
            }

            _orders = assetPairQuotes;

            return TaskEx.Empty;
        }

        public Task<AssetPairQuote> UpdateAsync(AssetPair assetPair)
        {
            var order = _orders.FirstOrDefault(o => o.AssetPairId == assetPair.Id);

            if (order == null)
                throw new InvalidOperationException();

            var significantDigit = Math.Pow(10, -assetPair.Accuracy);
            var sign = rnd.Next(0, 1) * 2 - 1;

            var ask = order.Ask + sign * significantDigit;
            var bid = order.Bid + sign * significantDigit;

            order.DateTime = DateTime.UtcNow;
            order.Ask = ask;
            order.Bid = bid;

            return Task.FromResult(order);
        }
    }
}