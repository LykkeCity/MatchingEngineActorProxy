using System.Fabric;
using Lykke.Core.Domain.Dictionary;

namespace MatchingEngineActor.Proxy
{
    public interface IProxy
    {
        IDictionaryService Connect(StatefulServiceContext context);
    }
}