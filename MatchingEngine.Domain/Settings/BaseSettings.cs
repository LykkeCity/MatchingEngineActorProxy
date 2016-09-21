namespace MatchingEngine.Domain.Settings
{
    public class BaseSettings
    {
        public MatchingOrdersSettings MatchingEngine { get; set; }

        public FactorySettings Factories { get; set; }
    }

    public class FactorySettings
    {
        public string DictionaryFactoryUri { get; set; }
    }

    public class MatchingOrdersSettings
    {
        public string ServiceBusConnectionString { get; set; }
    }
}