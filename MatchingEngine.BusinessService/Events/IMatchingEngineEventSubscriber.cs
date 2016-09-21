using System.Threading.Tasks;
using Lykke.Core.Domain.MatchingEngine;
using Microsoft.ServiceFabric.Actors;

namespace MatchingEngine.BusinessService.Events
{
    public interface IMatchingEngineEventSubscriber : IMatchingEngineEvents
    {
        Task SubscribeAsync(string subscriber);

        Task UnsubscribeAsync(string subscriber, string topicName);
    }
}