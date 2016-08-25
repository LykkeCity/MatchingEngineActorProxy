using System.Fabric;
using Lykke.Core.Domain.Dictionary;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace MatchingEngineActor.Proxy
{
    public class DictionaryProxy : IProxy
    {
        public IDictionaryService Connect(StatefulServiceContext context)
        {
            var config = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = config.Settings.Sections["ActorSettings"];

            var actorServiceUri = section.Parameters["DictionaryServiceUrl"].Value;

            var actorId = ActorId.CreateRandom();
            var proxy = ActorProxy.Create<IDictionaryService>(actorId, actorServiceUri);

            return proxy;
        }
    }
}