using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.Account;
using Lykke.Core.Domain.Account.Models;
using MatchingEngine.Utils.Extensions;

namespace MatchingEngine.DataAccess.Account
{
    public class AccountInfoRepository : IAccountInfoRepository
    {
        private const double _defaultBalance = 50000;
        private const double _defaultShoulder = 200;
        private const string _defaultBaseAssetId = "EUR";
        private static readonly Dictionary<string, AccountInfo> _accounts = new Dictionary<string, AccountInfo>();

        public Task DeteleAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task<AccountInfo> GetByIdAsync(string accountId)
        {
            if (!_accounts.ContainsKey(accountId))
            {
                var newAccountInfo = new AccountInfo
                {
                    AccountId = accountId,
                    Balance = _defaultBalance,
                    BaseAssetId = _defaultBaseAssetId,
                    Leverage = _defaultShoulder
                };
                await AddAsync(newAccountInfo);
            }

            return _accounts[accountId];
        }

        public async Task UpdateAsync(AccountInfo accountInfo)
        {
            var currentAccountInfo = await GetByIdAsync(accountInfo.AccountId);

            _accounts[currentAccountInfo.AccountId] = accountInfo;
        }

        public Task AddAsync(AccountInfo accountInfo)
        {
            if (_accounts.ContainsKey(accountInfo.AccountId))
                throw new InvalidOperationException();

            _accounts.Add(accountInfo.AccountId, accountInfo);

            return TaskEx.Empty;
        }

        public async Task<IEnumerable<AccountInfo>> GetAllAsync()
        {
            if (_accounts.Count == 0)
            {
                await GetByIdAsync(Guid.NewGuid().ToString());
            }

            return _accounts.Values;
        }
    }
}