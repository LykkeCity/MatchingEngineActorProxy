using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Core.Domain.Exchange.Models;

namespace MatchingEngine.BusinessService.Exchange
{
    public interface IOrderBookService
    {
        Task<IEnumerable<OrderBook>> BuildOrderBookAsync();
    }
}