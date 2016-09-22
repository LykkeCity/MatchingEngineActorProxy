using System;
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
        private readonly Random _rnd = new Random();

        public Task<IEnumerable<AssetPairQuote>> GetAllAsync()
        {
            return Task.FromResult(_orders);
        }

        public Task AddAsync(AssetPairQuote entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(AssetPairQuote assetPairQuote)
        {
            var order = _orders.FirstOrDefault(o => o.AssetPairId == assetPairQuote.AssetPairId);

            if (order == null)
            {
                throw new InvalidOperationException();
            }

            order = assetPairQuote;
            order.DateTime = DateTime.UtcNow;

            return TaskEx.Empty;
        }

        public Task DeteleAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task AddAllAsync(IEnumerable<AssetPair> assetPairs)
        {
            var assetPairQuotes = new List<AssetPairQuote>();

            foreach (var assetPair in assetPairs)
            {
                var ask = _rnd.NextDouble()*(3 - 0.1) + 0.1;
                var bid = ask - _rnd.NextDouble();

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
            {
                throw new InvalidOperationException();
            }

            var significantDigit = Math.Pow(10, -assetPair.Accuracy);
            var sign = _rnd.Next(0, 1)*2 - 1;

            var ask = order.Ask + sign*significantDigit;
            var bid = order.Bid + sign*significantDigit;

            order.DateTime = DateTime.UtcNow;
            order.Ask = ask;
            order.Bid = bid;

            return Task.FromResult(order);
        }

        public Task<AssetPairQuote> GetByIdAsync(string id)
        {
            var quote = _orders.FirstOrDefault(x => x.AssetPairId == id);

            return Task.FromResult(quote);
        }
    }
}