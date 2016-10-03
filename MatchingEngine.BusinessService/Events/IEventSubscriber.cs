using System.Threading.Tasks;

namespace MatchingEngine.BusinessService.Events
{
    public interface IEventSubscriber
    {
        Task SubscribeAsync(string subscriber);

        Task UnsubscribeAsync(string subscriber, string topicName);
    }
}