using System;

namespace MatchingEngine.Core.Exchange
{
    public enum OrderAction
    {
        Buy,
        Sell
    }

    public enum OrderStatus
    {
        //Init status, limit order in order book
        InOrderBook,
        //Partially matched
        Processing,
        //Fully matched
        Matched,
        //Not enough funds on account
        NotEnoughFunds,
        //No liquidity
        NoLiquidity,
        //Unknown asset
        UnknownAsset,
        //Cancelled
        Cancelled
    }


    public interface IOrderBase
    {
        string Id { get; }
        string ClientId { get; }
        DateTime CreatedAt { get; }
        double Volume { get; }
        double Price { get; }
        string AssetPairId { get; }
        string Status { get; }
        bool Straight { get; }
    }

    public static class BaseOrderExt
    {
        public static OrderAction OrderAction(this IOrderBase orderBase)
        {
            return orderBase.Volume > 0 ? Exchange.OrderAction.Buy : Exchange.OrderAction.Sell;
        }
    }
}