using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace MatchingEngine.Core.Exchange
{
    public interface IMatchingEngine : IActor, IActorEventPublisher<IMatchingEngineEvents>
    {
        Task InitAsync();

        Task<string> HandleMarketOrderAsync(string clientId, string assetPairId, OrderAction orderAction, double volume,
            bool straight);

        Task HandleLimitOrderAsync(string clientId, string assetPairId, OrderAction orderAction, double volume,
            double price);

        Task<CashInOutResponse> CashInOutBalanceAsync(string clientId, string assetId, double balanceDelta,
            bool sendToBlockchain, string correlationId);

        Task UpdateBalanceAsync(string clientId, string assetId, double value);

        Task CancelLimitOrderAsync(int orderId);

        /// <summary>
        ///     Update Wallet Credentials cache in ME
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns>True if update was successfull</returns>
        Task<bool> UpdateWalletCredsForClient(string clientId);
    }
}