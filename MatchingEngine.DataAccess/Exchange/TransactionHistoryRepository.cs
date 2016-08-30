using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets.Models;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Exchange
{
    public class TransactionHistoryRepository : ITransactionHistoryRepository
    {
        private static readonly Dictionary<string, List<TransactionHistory>> _transactions =
            new Dictionary<string, List<TransactionHistory>>();

        public Task AddAsync(OrderInfo orderInfo, AssetPairQuote currentQuote)
        {
            var transactionHistory = new TransactionHistory
            {
                AccountId = orderInfo.ClientId,
                AssetPairId = orderInfo.AssetPairId,
                CompletedAt = DateTime.UtcNow,
                ProfitLoss = 0,
                TransactionId = orderInfo.Id,
                Price = orderInfo.OrderAction == OrderAction.Buy ? currentQuote.Ask : currentQuote.Bid
            };

            if (_transactions.Count == 0 || !_transactions.ContainsKey(orderInfo.ClientId))
            {
                _transactions.Add(orderInfo.ClientId, new List<TransactionHistory> {transactionHistory});
            }
            else
            {
                if (_transactions.ContainsKey(orderInfo.ClientId))
                    _transactions[transactionHistory.AccountId].Add(transactionHistory);
            }

            return TaskEx.Empty;
        }

        public Task<IEnumerable<TransactionHistory>> GetAllAsync(string accountId)
        {
            if (!_transactions.ContainsKey(accountId))
                return TaskEx.Null<IEnumerable<TransactionHistory>>();

            return Task.FromResult<IEnumerable<TransactionHistory>>(_transactions[accountId]);
        }
    }
}