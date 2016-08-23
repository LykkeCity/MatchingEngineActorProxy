using System;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace MatchingEngineActor
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<MatchingEngine>(
                        (context, actorType) => new ActorService(context, actorType, () => new MatchingEngine()))
                    .GetAwaiter()
                    .GetResult();

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