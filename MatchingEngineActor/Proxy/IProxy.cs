using System.Fabric;
using Core.Domain.Dictionary;

namespace MatchingEngineActor.Proxy
{
    public interface IProxy
    {
        IDictionaryService Connect(StatefulServiceContext context);
    }
}