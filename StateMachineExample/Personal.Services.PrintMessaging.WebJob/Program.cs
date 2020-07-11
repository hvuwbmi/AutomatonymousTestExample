// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="West Bend">
//   Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.WebJob
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using MassTransit;
    using MessageService.Core;
    using Microsoft.Azure.WebJobs;
    using Personal.Contracts.CommonLog;
    using Personal.Service.Api.Logger;
    using Refit;
    using Saga;
    using Saga.ServiceInterfaces;

    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(config);
            
            var logger = new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey);
            logger.Log(
                new Log()
            {
                Messages = new List<LogMessage>()
                {
                    new LogMessage()
                    {
                        Category = Constants.Logging.CATEGORY,
                        Severity = SeverityType.Information,
                        Message = $"{Constants.Service.NAME} is starting..",
                    }
                }
            });
            var printJobService = RestService.For<IPrintJobService>(Settings.ApimURL);
            var initializer = new PrintMessagingServiceBusInitializer(new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey), new MessagingService(new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey), Bus.Factory), printJobService);
            try
            {
                var transactionCommittedTopicSubscribers = new List<TopicSubscriber>()
                {
                    new TopicSubscriber("TransactionCommitted", "DuckCreekPrintQueueSubscriber"),
                    new TopicSubscriber("Personal.Services.StateMachineMessageContracts.Messages.PrintMessaging/IPrintJobStateMessageEvent", "PL.IPrintJobStateMessageEventSubscriber")
                };

                var transactionSubmittedTopicSubscribers = new List<TopicSubscriber>()
                {
                    new TopicSubscriber("TransactionSubmitted", "DuckCreekPrintQueueSubscriber"),
                    new TopicSubscriber("Personal.Services.StateMachineMessageContracts.Messages.PrintMessaging/ITransactionSubmittedMessageEvent", "PL.ITransactionSubmittedMessageEventSubscriber")
                };

                var billingTopicSubscribers = new List<TopicSubscriber>()
                {
                    new TopicSubscriber("Billing-AccountActivity", "DuckCreekPrintQueueSubscriber"),
                    new TopicSubscriber("Personal.Services.StateMachineMessageContracts.Messages.BillingMessaging/IBillingMessageEvent", "PL.IBillingPrintMessageEventSubscriber")
                };

                var transactionScheduledTopicSubscribers = new List<TopicSubscriber>()
                {
                    new TopicSubscriber("pl-transactionscheduled", "DuckCreekPrintQueueSubscriber"),
                    new TopicSubscriber("Personal.Services.StateMachineMessageContracts.Messages.PrintMessaging/ITransactionScheduledMessageEvent", "PL.ITransactionScheduledMessageEventSubscriber")
                };

                IBusControl transactionCommittedBus = initializer.RegisterTransactionCommitPrintBusInstance(GetServiceBusSettings(transactionCommittedTopicSubscribers));
                transactionCommittedBus.Start();

                IBusControl transactionSubmittedBus = initializer.RegisterTransactionSubmitPrintBusInstance(GetServiceBusSettings(transactionSubmittedTopicSubscribers));
                transactionSubmittedBus.Start();

                IBusControl billingPrintBus = initializer.RegisterBillingPrintBusInstance(GetServiceBusSettings(billingTopicSubscribers));
                billingPrintBus.Start();

                IBusControl transactionScheduledBus = initializer.RegisterTransactionScheduledPrintBusInstance(GetServiceBusSettings(transactionScheduledTopicSubscribers));
                transactionScheduledBus.Start();
            }
            catch (Exception ex)
            {
                logger.Log(new Log()
                {
                    Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Error,
                            Message = ex.ToString(),
                        }
                    }
                });
                throw;
            }

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();

            host.Stop();
        }

        private static ServiceBusSettings GetServiceBusSettings(string topic, string subscriber)
        {
            return new ServiceBusSettings
            {
                Key = ConfigurationManager.AppSettings["AzureSbSharedAccessKey"],
                KeyName = ConfigurationManager.AppSettings["AzureSbKeyName"],
                NameSpace = ConfigurationManager.AppSettings["AzureSbNamespace"],
                Path = ConfigurationManager.AppSettings["AzureSbPath"],
                Subscriber = subscriber,
                Topic = topic
            };
        }

        private static List<ServiceBusSettings> GetServiceBusSettings(List<TopicSubscriber> topicSubscribers)
        {
            var serviceBusSettings = new List<ServiceBusSettings>();
            foreach (var topicSubscriber in topicSubscribers)
            {
                serviceBusSettings.Add(new ServiceBusSettings
                {
                    Key = ConfigurationManager.AppSettings["AzureSbSharedAccessKey"],
                    KeyName = ConfigurationManager.AppSettings["AzureSbKeyName"],
                    NameSpace = ConfigurationManager.AppSettings["AzureSbNamespace"],
                    Path = ConfigurationManager.AppSettings["AzureSbPath"],
                    Subscriber = topicSubscriber.Subscriber,
                    Topic = topicSubscriber.Topic
                });
            }

            return serviceBusSettings;
        }
    }
}
