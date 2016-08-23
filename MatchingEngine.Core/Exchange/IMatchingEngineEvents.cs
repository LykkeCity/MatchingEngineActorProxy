using Microsoft.ServiceFabric.Actors;

namespace MatchingEngine.Core.Exchange
{
    public interface IMatchingEngineEvents : IActorEvents
    {
        void BalanceUpdated(double balance);
    }
}