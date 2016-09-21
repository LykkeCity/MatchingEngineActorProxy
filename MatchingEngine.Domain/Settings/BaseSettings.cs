namespace MatchingEngine.Domain.Settings
{
    public class BaseSettings
    {
        public MatchingOrdersSettings MatchingEngine { get; set; }
    }

    public class MatchingOrdersSettings
    {
        public string ServiceBusConnectionString { get; set; }
    }
}