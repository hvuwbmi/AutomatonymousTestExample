// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintController.cs" company="West Bend">
//   Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Personal.Contracts.CommonLog;
    using Personal.Service.Api.Logger;
    using Personal.Services.PrintMessaging.Saga.MessageStates;
    using Personal.Services.PrintMessaging.Saga.ServiceInterfaces;

    public class PrintController
    {
        private string apimSubscriptionKey;
        private IPrintJobService printJobService;
        private IMessageLogger logger;

        public PrintController(IPrintJobService printJobService, IMessageLogger logger)
        {
            this.printJobService = printJobService;
            this.logger = logger;
        }

        public string ApimSubscriptionKey
        {
            get
            {
                return this.apimSubscriptionKey ?? (this.apimSubscriptionKey = ConfigurationManager.AppSettings["ApimSubscriptionKey"]);
            }

            set
            {
                this.apimSubscriptionKey = value;
            }
        }

        internal Task<string> QueueCancelPendingPrint(BillingMessageState instance)
        {
            var transactionSequenceNumber = this.GetPolicyTransactionSequenceNumber(instance.PolicyNumber, instance.PolicyVersion);

            var printJobName = string.Empty;

            if (instance.ActivityTypeCode == Constants.TransactionType.PENDINGCANCEL)
            {
                printJobName = Constants.PrintJobNames.CANCELPENDING;
            }

            if (instance.ActivityTypeCode == Constants.BillingActivityTypeCodes.RESCINDPCN)
            {
                printJobName = Constants.PrintJobNames.RESCINDCANCELPENDING;
            }
            
            return this.printJobService.PostPrintJobRequest(instance.PolicyNumber, instance.PolicyVersion, transactionSequenceNumber, printJobName, instance.AdditionalData, this.ApimSubscriptionKey);
        }

        internal string GetPolicyTransactionSequenceNumber(string policyNumber, string policyVersion)
        {
            int transactionSequenceNumber = 1;

            try
            {
                var transactions = this.printJobService.GetPolicyTransactions(policyNumber, this.ApimSubscriptionKey).Result;
                var policyTransactions = XDocument.Parse(transactions.ToString());

                var currentPolicyTransactionSequenceNumbers = policyTransactions.XPathSelectElements($"/transactions/transaction[PolicyVersion='{policyVersion}' and Status='Committed']/TransactionCounter");

                int value = 0;
                foreach (var currentTransactionSequenceNumber in currentPolicyTransactionSequenceNumbers)
                {
                    if (int.TryParse(currentTransactionSequenceNumber.Value, out value))
                    {
                        transactionSequenceNumber = transactionSequenceNumber < value ? value : transactionSequenceNumber;
                    }
                }
            }
            catch (Exception exception)
            {
                this.logger.Log(
                    new Log()
                {
                    Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Error,
                            Message = $"GetPolicyTransactionSequenceNumber failed for policy {policyNumber}-{policyVersion}",
                            LogAttributes = new List<LogAttribute>()
                            {
                                new LogAttribute()
                                {
                                    Key = "Exception",
                                    Value = exception.ToString(),
                                }
                            }
                        }
                    }
                }, 
                    policyNumber, 
                    policyVersion);
                throw;
            }

            return transactionSequenceNumber.ToString();
        }
    }
}
