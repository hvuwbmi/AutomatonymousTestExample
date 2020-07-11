// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintMessagingServiceBusInitializer.cs" company="West Bend">
//   Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.WebJob
{
    using System.Collections.Generic;
    using MassTransit;
    using MessageService.Core;
    using Personal.Service.Api.Logger;
    using Saga;
    using Saga.ServiceInterfaces;

    public class PrintMessagingServiceBusInitializer
    {
        private readonly IMessageService messageService;
        private IPrintJobService printJobService;
        private IMessageLogger logger;

        public PrintMessagingServiceBusInitializer(IMessageLogger logger, IMessageService messagingService, IPrintJobService printJobService)
        {
            this.messageService = messagingService;
            this.printJobService = printJobService;
            this.logger = logger;
        }

        public IBusControl RegisterTransactionCommitPrintBusInstance(List<ServiceBusSettings> serviceBusSettings)
        {
            var bus = this.messageService.Initialize(new PrintJobStateMessageMachine(this.printJobService), Constants.Service.NAME, Constants.Logging.CATEGORY, serviceBusSettings, string.Empty);
            return bus;
        }

        public IBusControl RegisterTransactionSubmitPrintBusInstance(List<ServiceBusSettings> serviceBusSettings)
        {
            var bus = this.messageService.Initialize(new TransactionSubmittedMessageMachine(this.printJobService), Constants.Service.NAME, Constants.Logging.CATEGORY, serviceBusSettings, string.Empty);
            return bus;
        }

        public IBusControl RegisterBillingPrintBusInstance(List<ServiceBusSettings> serviceBusSettings)
        {
            var bus = this.messageService.Initialize(new BillingMessageMachine(this.printJobService), Constants.Service.NAME, Constants.Logging.CATEGORY, serviceBusSettings, string.Empty);
            return bus;
        }

        public IBusControl RegisterTransactionScheduledPrintBusInstance(List<ServiceBusSettings> serviceBusSettings)
        {
            var bus = this.messageService.Initialize(new TransactionScheduledMessageMachine(this.printJobService), Constants.Service.NAME, Constants.Logging.CATEGORY, serviceBusSettings, string.Empty);
            return bus;
        }
    }
}
