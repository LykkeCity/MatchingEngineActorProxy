using AutoMapper;
using Lykke.Core.Domain.Exchange.Models;

namespace MatchingEngine.Actor
{
    public class MappingConfig
    {
        public static void Configure()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ActorProfile());
            });
        }

        public class ActorProfile : Profile
        {
            public ActorProfile()
            {
                CreateMap<MarketOrder, OrderInfo>();
                CreateMap<PendingOrder, OrderInfo>();

            }
        }
    }
}