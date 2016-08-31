using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.Exchange;
using Lykke.Core.Domain.Exchange.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Exchange
{
    public class TransactionHistoryRepository : ITransactionHistoryRepository
    {
        private static readonly Dictionary<string, List<TransactionHistory>> _transactions =
            new Dictionary<string, List<TransactionHistory>>();

        public Task AddAsync(TransactionHistory transactionHistory)
        {
            if (_transactions.Count == 0 || !_transactions.ContainsKey(transactionHistory.AccountId))
            {
                _transactions.Add(transactionHistory.AccountId, new List<TransactionHistory> {transactionHistory});
            }
            else
            {
                if (_transactions.ContainsKey(transactionHistory.AccountId))
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