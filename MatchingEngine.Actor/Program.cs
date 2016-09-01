using System;
using System.Threading;
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
                        (context, actorType) => new ActorService(context, actorType, () => new MatchingEngine(context)))
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