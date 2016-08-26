using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Exchange
{
    public class OrderInfoRepository : IOrderInfoRepository
    {
        private static readonly Dictionary<string, List<OrderInfo>> _orders = new Dictionary<string, List<OrderInfo>>();
        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;

        public OrderInfoRepository(IAssetPairQuoteRepository assetPairQuoteRepository)
        {
            _assetPairQuoteRepository = assetPairQuoteRepository;
        }

        public async Task AddAsync(string accountId, string assetPairId, double volume)
        {
            var currentQuote = await _assetPairQuoteRepository.GetAsync(assetPairId);

            if (currentQuote == null)
                throw new InvalidOperationException();

            var orderInfo = new OrderInfo
            {
                ClientId = accountId,
                AssetPairId = assetPairId,
                Volume = volume,
                Id = Guid.NewGuid().ToString(),
                CreatedAt = currentQuote.DateTime
            };

            orderInfo.Price = OrderInfo.OrderAction(orderInfo) == OrderAction.Buy ? currentQuote.Ask : currentQuote.Bid;

            if (_orders.ContainsKey(accountId))
            {
                _orders[accountId].Add(orderInfo);
            }
            else
            {
                _orders.Add(accountId, new List<OrderInfo> {orderInfo});
            }
        }

        public Task<IEnumerable<OrderInfo>> GetAllAsync(string accountId)
        {
            if (_orders.Count == 0)
                return TaskEx.Null<IEnumerable<OrderInfo>>();

            IEnumerable<OrderInfo> accountOrders = _orders[accountId];

            return Task.FromResult(accountOrders);
        }

        public Task DeleteAsync(string orderId)
        {
            throw new System.NotImplementedException();
        }
    }
}