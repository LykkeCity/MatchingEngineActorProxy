using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.BusinessService.Proxy;

namespace MatchingEngine.BusinessService.Exchange
{
    public class OrderBookService : IOrderBookService
    {
        private static IEnumerable<OrderBook> _orderBooks = new List<OrderBook>();
        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;
        private readonly IDictionaryProxy _dictionaryProxy;
        private readonly Random _rnd = new Random();

        public OrderBookService(IAssetPairQuoteRepository assetPairQuoteRepository, IDictionaryProxy dictionaryProxy)
        {
            _assetPairQuoteRepository = assetPairQuoteRepository;
            _dictionaryProxy = dictionaryProxy;
        }

        public async Task<IEnumerable<OrderBook>> BuildOrderBookAsync()
        {
            var orderBook = _orderBooks.Any() ? _orderBooks : _orderBooks = await GetOrderBooks();

            return orderBook;
        }

        private async Task<List<OrderBook>> GetOrderBooks()
        {
            var assetPairQuotes = await _assetPairQuoteRepository.GetAllAsync();

            var orderBook = new List<OrderBook>();

            foreach (var assetPairQuote in assetPairQuotes)
            {
                var assetPair = await _dictionaryProxy.GetAssetPairByIdAsync(assetPairQuote.AssetPairId);

                var significantDigit = Math.Pow(10, -assetPair.Accuracy);
                var sign = _rnd.Next(0, 1)*2 - 1;

                var ask = assetPairQuote.Ask + sign*significantDigit;
                var bid = assetPairQuote.Bid + sign*significantDigit;

                var orderBookItemSell = new OrderBook
                {
                    AssetPair = assetPair,
                    Price = ask,
                    OrderAction = OrderAction.Sell,
                    Volume = -1*_rnd.Next(500, 100000)
                };

                var orderBookItemBuy = new OrderBook
                {
                    AssetPair = assetPair,
                    Price = bid,
                    OrderAction = OrderAction.Buy,
                    Volume = _rnd.Next(500, 100000)
                };

                orderBook.Add(orderBookItemSell);
                orderBook.Add(orderBookItemBuy);
            }
            return orderBook;
        }
    }
}