using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Core.Domain.MatchingEngine;
using MatchingEngine.Utils.Extensions;
using Microsoft.ServiceFabric.Actors;

namespace MatchingEngine.Actor.Events
{
    public class EventSubscriber : IEventSubscriber
    {
        private static readonly ConcurrentDictionary<ActorId, IMatchingEngineEventSubscriber> _eventSubscribers =
            new ConcurrentDictionary<ActorId, IMatchingEngineEventSubscriber>();

        public Task SubscribeAsync(IMatchingEngineEventSubscriber events)
        {
            _eventSubscribers.AddOrUpdate(events.GetActorReference().ActorId, events, (key, existingVal) => events);
            return TaskEx.Empty;
        }

        public Dictionary<ActorId, IMatchingEngineEventSubscriber> GetActiveSubsribers()
        {
            return _eventSubscribers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void Unsubscribe(KeyValuePair<ActorId, IMatchingEngineEventSubscriber> subscriber)
        {
            if (_eventSubscribers.ContainsKey(subscriber.Key))
            {
                IMatchingEngineEventSubscriber value;
                if (!_eventSubscribers.TryRemove(subscriber.Key, out value))
                {
                    //todo
                }
            }
        }
    }
}