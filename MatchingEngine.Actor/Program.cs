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
                ActorRuntime.RegisterActorAsync<global::MatchingEngine.Actor.MatchingEngine>(
                        (context, actorType) => new ActorService(context, actorType, () => new global::MatchingEngine.Actor.MatchingEngine(context)))
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