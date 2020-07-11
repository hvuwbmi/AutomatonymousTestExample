// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BillingMessageMachine.cs" company="West Bend">
//   Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga
{
    using System;
    using System.Collections.Generic;
    using Automatonymous;
    using MessageStates;
    using Personal.Contracts.CommonLog;
    using Personal.Service.Api.Logger;
    using Personal.Services.PrintMessaging.Saga.Controllers;
    using ServiceInterfaces;
    using WestBend.Billing.ServiceContracts.EventMessages;

    public class BillingMessageMachine : PrintJobMassTransitStateMachine<BillingMessageState>
    {
        private IPrintJobService printJobService;
        private IMessageLogger logger;

        public BillingMessageMachine(IPrintJobService printJobService) : 
            this(printJobService, new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey))
        {
            this.printJobService = printJobService;
        }

        public BillingMessageMachine(IPrintJobService printJobService, IMessageLogger logger) : this()
        {
            this.printJobService = printJobService;
            this.logger = logger;
        }

        protected BillingMessageMachine()
        {
            this.InstanceState(x => x.CurrentState);

            this.Event(() => this.BillingAccountActivity, x => x.CorrelateBy(state => $"{state.PolicyNumber}/{state.PolicyVersion}", context => $"{context.Message.Policy.PolicyNumber}/{context.Message.Policy.Mod}").SelectId(context => Guid.NewGuid()));

            this.Initially(
                When(this.BillingAccountActivity, x => this.IsValidActivityType(x.Data.ActivityTypeCode, x.Data.Policy.PolicyNumber, this.logger) && this.IsValidIssueSystemCode(x.Data.Policy.IssueSystemCode, x.Data.Policy.PolicyNumber, this.logger))
                    .Then(context => AssignStateFromEvent(context.Instance, context.Data))
                    .Then(context => this.Log($"Transaction print job started for {context.Instance.ActivityTypeCode} for {context.Instance.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => new PrintController(this.printJobService, this.logger).QueueCancelPendingPrint(context.Instance))
                    .Then(context => this.Log($"Transaction print job ended for {context.Instance.ActivityTypeCode} for {context.Instance.PolicyNumber}", context, this.logger))
                    .Finalize());

            this.SetCompletedWhenFinalized();
        }

        public Event<IAccountActivity> BillingAccountActivity { get; private set; }

        internal bool IsValidActivityType(string activityTypeCode, string policyNumber, IMessageLogger log)
        {
            bool success = activityTypeCode.Equals(Constants.TransactionType.PENDINGCANCEL, StringComparison.InvariantCultureIgnoreCase)
                || activityTypeCode.Equals(Constants.BillingActivityTypeCodes.RESCINDPCN, StringComparison.InvariantCultureIgnoreCase);

            if (!success)
            {
                log.Log(
                    new Log()
                {
                    Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Information,
                            Message = $"Detected an activity type to not process: {activityTypeCode} for {policyNumber}",
                        }
                    }
                }, 
                    policyNumber);
            }

            return success;
        }

        internal bool IsValidIssueSystemCode(string issueSystemCode, string policyNumber, IMessageLogger log)
        {
            bool success = issueSystemCode.Equals(Constants.IssueSystemCode.DUCKPERSONAL, StringComparison.InvariantCultureIgnoreCase);

            if (!success)
            {
                log.Log(
                    new Log()
                    {
                        Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Information,
                            Message = $"Detected a system code to not process: {issueSystemCode} for {policyNumber}",
                        }
                    }
                },
                    policyNumber);
            }

            return success;
        }

        private static void AssignStateFromEvent(BillingMessageState state, IAccountActivity billingEvent)
        {
            state.ActivityTypeCode = billingEvent.ActivityTypeCode;
            state.PolicyNumber = billingEvent.Policy.PolicyNumber;
            state.PolicyVersion = billingEvent.Policy.Mod;
            state.AdditionalData = DateTime.Now.ToString("yyyyMMddhhmmss");
            state.Step = 0;
            state.StateMachineName = "BillingMessageMachine";
        }
    }
}