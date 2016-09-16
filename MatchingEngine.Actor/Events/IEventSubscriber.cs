using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.MatchingEngine;
using Microsoft.ServiceFabric.Actors;

namespace MatchingEngine.Actor.Events
{
    public interface IEventSubscriber
    {
        Task SubscribeAsync(IMatchingEngineEventSubscriber events);

        Dictionary<ActorId, IMatchingEngineEventSubscriber> GetActiveSubsribers();

        void Unsubscribe(KeyValuePair<ActorId, IMatchingEngineEventSubscriber> subscriber);
    }
}