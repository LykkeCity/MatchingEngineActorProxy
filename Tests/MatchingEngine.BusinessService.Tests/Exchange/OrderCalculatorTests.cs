using System;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets;
using Lykke.Core.Domain.Assets.Models;
using MatchingEngine.BusinessService.Exchange;
using MatchingEngine.BusinessService.Proxy;
using NSubstitute;
using NUnit.Framework;

namespace MatchingEngine.BusinessService.Tests.Exchange
{
    [TestFixture]
    public class OrderCalculatorTests
    {
        [Test]
        public async Task BasePair_IsQuotingAsset_In_AssetPair_CorrectCalculation()
        {
            //data
            var openPrice = 1.4351;
            var closePrice = 1.4361;
            var volume = 1000;
            var baseAssetId = "USD";
            var assetPair=  new AssetPair
            {
                Id = "GBPUSD",
                BaseAssetId = "GBP",
                QuotingAssetId = "USD"
            };

            //arrange
            var orderCalculator = MockOrderCalculator();
            var profitLoss = await orderCalculator.CalculateProfitLossAsync(openPrice, closePrice, volume, assetPair, baseAssetId);

            Assert.That(Math.Round(profitLoss, 4), Is.EqualTo(1.0));
        }

        [Test]
        public async Task BasePair_IsBaseAsset_In_AssetPair_CorrectCalculation()
        {
            //data
            var openPrice = 1.4351;
            var closePrice = 1.4361;
            var volume = 1000;
            var baseAssetId = "USD";
            var assetPair = new AssetPair
            {
                Id = "USDCHF",
                BaseAssetId = "USD",
                QuotingAssetId = "CHF"
            };

            //arrange
            var orderCalculator = MockOrderCalculator();
            var profitLoss = await orderCalculator.CalculateProfitLossAsync(openPrice, closePrice, volume, assetPair, baseAssetId);

            Assert.That(Math.Round(profitLoss, 4), Is.EqualTo(0.8182));
        }

        [Test]
        public async Task BasePair_IsNot_In_AssetPair_AsserPairExists_CorrectCalculation()
        {
            //data
            var openPrice = 1.4351;
            var closePrice = 1.4361;
            var volume = 1000;
            var baseAssetId = "USD";
            var assetPair = new AssetPair
            {
                Id = "EURJPY",
                BaseAssetId = "EUR",
                QuotingAssetId = "JPY"
            };

            //arrange
            var orderCalculator = MockOrderCalculator();
            var profitLoss = await orderCalculator.CalculateProfitLossAsync(openPrice, closePrice, volume, assetPair, baseAssetId);

            Assert.That(Math.Round(profitLoss, 4), Is.EqualTo(0.75));
        }

        [Test]
        public async Task BasePair_IsNot_In_AssetPair_AsserPairDoesntExist_CorrectCalculation()
        {
            //data
            var openPrice = 1.4351;
            var closePrice = 1.4361;
            var volume = 1000;
            var baseAssetId = "USD";
            var assetPair = new AssetPair
            {
                Id = "EURGBP",
                BaseAssetId = "EUR",
                QuotingAssetId = "GBP"
            };

            //arrange
            var orderCalculator = MockOrderCalculator();
            var profitLoss = await orderCalculator.CalculateProfitLossAsync(openPrice, closePrice, volume, assetPair, baseAssetId);

            Assert.That(Math.Round(profitLoss, 4), Is.EqualTo(1.5555));
        }

        private OrderCalculator MockOrderCalculator()
        {
            var assetPairQuoteRepository = Substitute.For<IAssetPairQuoteRepository>();
            var gbpUsd = new AssetPairQuote
            {
                AssetPairId = "GBPUSD",
                Ask = 1.5555
            };
            var usdChf = new AssetPairQuote
            {
                AssetPairId = "USDCHF",
                Ask = 1.2222
            };
            var usdJpy = new AssetPairQuote
            {
                AssetPairId = "USDJPY",
                Ask = 1.3333
            };
            assetPairQuoteRepository.GetAsync(gbpUsd.AssetPairId).Returns(gbpUsd);
            assetPairQuoteRepository.GetAsync(usdChf.AssetPairId).Returns(usdChf);
            assetPairQuoteRepository.GetAsync(usdJpy.AssetPairId).Returns(usdJpy);

            var dictionaryProxy = Substitute.For<IDictionaryProxy>();
            dictionaryProxy.GetAssetPairAsync("USD", "JPY").Returns(new AssetPair {Id = "USDJPY"});
            dictionaryProxy.GetAssetPairAsync("GBP", "USD").Returns(new AssetPair {Id = "GBPUSD" });

            var calculator = new OrderCalculator(assetPairQuoteRepository, dictionaryProxy);
            return calculator;
        }
    }
}