using System.Threading.Tasks;
using Lykke.Core.Domain.Assets.Models;

namespace MatchingEngine.BusinessService.Exchange
{
    public interface IOrderCalculator
    {
        Task<double> CalculateProfitLossAsync(double openPrice, double closePrice, double volume, AssetPair assetPair,
            string baseAssetId);
    }
}