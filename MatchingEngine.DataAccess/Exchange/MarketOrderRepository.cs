using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Exchange
{
    public class MarketOrderRepository : IMarketOrderRepository
    {
        private static readonly Dictionary<string, List<MarketOrder>> _orders =
            new Dictionary<string, List<MarketOrder>>();

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

        public Task AddAsync(MarketOrder entity)
        {
            if (_orders.ContainsKey(entity.ClientId))
                _orders[entity.ClientId].Add(entity);
            else
                _orders.Add(entity.ClientId, new List<MarketOrder> { entity });

            return TaskEx.Empty;
        }

        public Task UpdateAsync(MarketOrder entity)
        {
            throw new NotImplementedException();
        }
    }
}