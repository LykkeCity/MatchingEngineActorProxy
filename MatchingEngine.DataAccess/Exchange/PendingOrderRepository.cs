using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Exchange
{
    public class PendingOrderRepository : IPendingOrderRepository
    {
        private static readonly Dictionary<string, List<PendingOrder>> _orders =
            new Dictionary<string, List<PendingOrder>>();

        public Task AddAsync(string accountId, string assetPairId, double volume)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PendingOrder>> GetAllAsync(string accountId)
        {
            if (_orders.Count == 0)
                return TaskEx.Null<IEnumerable<PendingOrder>>();

            if (string.IsNullOrEmpty(accountId))
            {
                var allOrders = _orders.SelectMany(o => o.Value);
                return Task.FromResult(allOrders);
            }

            IEnumerable<PendingOrder> accountOrders = _orders[accountId];

            return Task.FromResult(accountOrders);
        }

        public Task<PendingOrder> GetAsync(string accountId, string orderId)
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

        public Task AddAsync(string accountId, string assetPairId, double volume, double definedPrice)
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

            if (_orders.ContainsKey(accountId))
                _orders[accountId].Add(orderInfo);
            else
                _orders.Add(accountId, new List<PendingOrder> {orderInfo});

            return TaskEx.Empty;
        }

        public Task<IEnumerable<PendingOrder>> FindByAssetPairIdAsync(string assetPairId)
        {
            if (_orders.Count == 0)
                return TaskEx.Null<IEnumerable<PendingOrder>>();

            return Task.FromResult(_orders.SelectMany(o => o.Value.Where(x => x.AssetPairId.Equals(assetPairId))));
        }
    }
}