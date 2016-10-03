using System.Threading.Tasks;
using Lykke.Core.Domain.MatchingEngine;
using Microsoft.ServiceFabric.Actors;

namespace MatchingEngine.BusinessService.Events
{
    public interface IMatchingEngineEventSubscriber : IMatchingEngineEvents, IEventSubscriber
    {
    }
}