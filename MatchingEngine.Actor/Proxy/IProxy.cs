using System.Fabric;
using Lykke.Core.Domain.Dictionary;

namespace MatchingEngine.Actor.Proxy
{
    public interface IProxy
    {
        IDictionaryService Connect(StatefulServiceContext context);
    }
}