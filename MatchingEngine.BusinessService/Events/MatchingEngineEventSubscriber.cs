using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Core.Domain.Assets.Models;
using Lykke.Core.Domain.MatchingEngine;
using MatchingEngine.Domain.Settings;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace MatchingEngine.BusinessService.Events
{
    public class MatchingEngineEventSubscriber : IMatchingEngineEventSubscriber
    {
        private readonly MatchingOrdersSettings _settings;

        public MatchingEngineEventSubscriber(MatchingOrdersSettings settingsMatchingEngine)
        {
            _settings = settingsMatchingEngine;
        }

        public async Task AccountUpdatedAsync(string accountId)
        {
            var message = new BrokeredMessage
            {
                Properties =
                {
                    {"accountId", accountId}
                }
            };

            await SendMessageAsync(MatchingEngineTopics.AccountUpdated, message);
        }

        public async Task AssetPairPriceUpdatedAsync(AssetPairQuote assetPair)
        {
            var message = new BrokeredMessage
            {
                Properties =
                {
                    {assetPair.AssetPairId, assetPair.AssetPairId},
                    {assetPair.Ask.ToString(CultureInfo.InvariantCulture), assetPair.Ask},
                    {assetPair.Bid.ToString(CultureInfo.InvariantCulture), assetPair.Bid},
                    {assetPair.DateTime.ToString(CultureInfo.InvariantCulture), assetPair.DateTime}
                }
            };

            await SendMessageAsync(MatchingEngineTopics.AssetPairPriceUpdated, message);
        }

        public async Task ActiveOrdersUpdatedAsync(string accountId)
        {
            var message = new BrokeredMessage
            {
                Properties =
                {
                    {"accountId", accountId}
                }
            };

            await SendMessageAsync(MatchingEngineTopics.ActiveOrdersUpdated, message);
        }

        public async Task SubscribeAsync(string subscriber)
        {
            foreach (var topic in MatchingEngineTopics.AllTopics)
                await CreateTopicSubscriptionAsync(topic, subscriber);
        }

        public async Task UnsubscribeAsync(string subscriber, string topicName)
        {
            await DeleteTopicSubscriptionAsync(topicName, subscriber);
        }

        private async Task SendMessageAsync(string topicName, BrokeredMessage message)
        {
            var topicClient = TopicClient.CreateFromConnectionString(_settings.ServiceBusConnectionString, topicName);

            await topicClient.SendAsync(message);
        }

        private async Task CreateTopicSubscriptionAsync(string topicName, string subscriptionName,
            bool requireSessions = false)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(_settings.ServiceBusConnectionString);
            if (namespaceManager.SubscriptionExists(topicName, subscriptionName))
            {
                if (namespaceManager.GetSubscription(topicName, subscriptionName).RequiresSession != requireSessions)
                {
                    //todo: add to log
                    Console.WriteLine($"Subscription '{subscriptionName}' will be deleted.");
                    await namespaceManager.DeleteSubscriptionAsync(topicName, subscriptionName);
                    Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine($"Subscription '{subscriptionName}' for Topic '{topicName}' exists.");
                    return;
                }
            }
            var description = new SubscriptionDescription(topicName, subscriptionName)
            {
                RequiresSession = requireSessions
            };
            await namespaceManager.CreateSubscriptionAsync(description);

            Console.WriteLine($"Created Subscription '{subscriptionName}' for Topic '{topicName}'.");
        }

        private async Task DeleteTopicSubscriptionAsync(string topicName, string subscriptionName,
            bool requireSessions = false)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(_settings.ServiceBusConnectionString);
            if (namespaceManager.SubscriptionExists(topicName, subscriptionName))
            {
                //todo: add to log
                Console.WriteLine($"Subscription '{subscriptionName}' will be deleted.");
                await namespaceManager.DeleteSubscriptionAsync(topicName, subscriptionName);
            }
        }
    }
}