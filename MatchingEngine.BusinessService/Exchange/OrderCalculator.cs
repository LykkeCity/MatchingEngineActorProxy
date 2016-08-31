using System.Threading.Tasks;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Assets.Models;
using MatchingEngine.BusinessService.Proxy;

namespace MatchingEngine.BusinessService.Exchange
{
    public class OrderCalculator : IOrderCalculator
    {
        private readonly IAssetPairQuoteRepository _assetPairQuoteRepository;
        private readonly IDictionaryProxy _dictionaryProxy;

        public OrderCalculator(IAssetPairQuoteRepository assetPairQuoteRepository, IDictionaryProxy dictionaryProxy)
        {
            _assetPairQuoteRepository = assetPairQuoteRepository;
            _dictionaryProxy = dictionaryProxy;
        }

        public async Task<double> CalculateProfitLossAsync(double openPrice, double closePrice, double volume, AssetPair assetPair,
            string baseAssetId)
        {
            var currencyRate = await GetCurrencyRateAsync(assetPair, baseAssetId);

            var result = (closePrice - openPrice)*volume/currencyRate;

            return result;
        }

        private async Task<double> GetCurrencyRateAsync(AssetPair assetPair, string baseAssetId)
        {
            if (assetPair.QuotingAssetId == baseAssetId)
                return 1;

            if ((assetPair.QuotingAssetId != baseAssetId) && (assetPair.BaseAssetId == baseAssetId))
            {
                var asset = await _assetPairQuoteRepository.GetAsync(assetPair.Id);

                return asset.Ask;
            }

            var requiredAssetPair = await _dictionaryProxy.GetAssetPairAsync(baseAssetId, assetPair.QuotingAssetId);

            if (requiredAssetPair != null)
            {
                var assetPairQuote = await _assetPairQuoteRepository.GetAsync(requiredAssetPair.Id);

                return assetPairQuote.Ask;
            }
            else
            {
                requiredAssetPair = await _dictionaryProxy.GetAssetPairAsync(assetPair.QuotingAssetId, baseAssetId);

                var assetPairQuote = await _assetPairQuoteRepository.GetAsync(requiredAssetPair.Id);

                return 1/assetPairQuote.Ask;
            }
        }
    }
}