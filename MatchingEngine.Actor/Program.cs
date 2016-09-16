using System;
using System.Threading;
using MatchingEngine.Actor.Events;
using MatchingEngine.BusinessService.Exchange;
using MatchingEngine.BusinessService.Proxy;
using MatchingEngine.DataAccess.Account;
using MatchingEngine.DataAccess.Asset;
using MatchingEngine.DataAccess.Exchange;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngine.Actor
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<MatchingEngine>(
                        (context, actorType) => new ActorService(context, actorType, () =>
                        {
                            var assetPairQuoteRepository = new AssetPairQuoteRepository();
                            var dictionaryProxy = new DictionaryProxy();

                            return new MatchingEngine(dictionaryProxy, new AccountInfoRepository(),
                                assetPairQuoteRepository, new MarketOrderRepository(assetPairQuoteRepository),
                                new PendingOrderRepository(), new TransactionHistoryRepository(),
                                new OrderCalculator(assetPairQuoteRepository, dictionaryProxy), new EventSubscriber());
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
    }
}