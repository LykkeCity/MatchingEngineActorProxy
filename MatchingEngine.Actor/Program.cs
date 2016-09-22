using System;
using System.Threading;
using MatchingEngine.BusinessService.Events;
using MatchingEngine.BusinessService.Exchange;
using MatchingEngine.BusinessService.Proxy;
using MatchingEngine.DataAccess;
using MatchingEngine.DataAccess.Account;
using MatchingEngine.DataAccess.Asset;
using MatchingEngine.DataAccess.Exchange;
using MatchingEngine.Domain.Settings;
using Microsoft.Azure;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngine.Actor
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                var settings = ReadSettings();

                ActorRuntime.RegisterActorAsync<MatchingEngine>(
                        (context, actorType) => new ActorService(context, actorType, () =>
                        {
                            var assetPairQuoteRepository = new AssetPairQuoteRepository();
                            var dictionaryProxy = new DictionaryProxy(settings.Factories);

                            return new MatchingEngine(dictionaryProxy, new AccountInfoRepository(),
                                assetPairQuoteRepository, new MarketOrderRepository(),
                                new PendingOrderRepository(), new TransactionHistoryRepository(),
                                new OrderCalculator(assetPairQuoteRepository, dictionaryProxy),
                                new MatchingEngineEventSubscriber(settings.MatchingEngine),
                                new OrderBookService(assetPairQuoteRepository, dictionaryProxy));
                        }))
                    .GetAwaiter()
                    .GetResult();

                MappingConfig.Configure();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static BaseSettings ReadSettings()
        {
            var connectionString = CloudConfigurationManager.GetSetting("ConnectionString");

            var containerSettingsLocation = CloudConfigurationManager.GetSetting("ContainerSettingsLocation");
            var containerSettingsKey = CloudConfigurationManager.GetSetting("ContainerSettingsKey");

            var settingsLocations = new Tuple<string, string>(containerSettingsLocation, containerSettingsKey);
            return GeneralSettingsReader.ReadGeneralSettings<BaseSettings>(connectionString, settingsLocations);
        }
    }
}