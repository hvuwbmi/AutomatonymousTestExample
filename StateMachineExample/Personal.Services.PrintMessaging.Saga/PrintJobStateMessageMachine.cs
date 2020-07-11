// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintJobStateMessageMachine.cs" company="West Bend">
//   Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga
{
    using System;
    using Automatonymous;
    using Personal.Service.Api.Logger;
    using ServiceInterfaces;
    using StateMachineMessageContracts.Messages.PrintMessaging;
    using WestBend.Personal.ServiceContracts.EventMessages;
    using WestBend.Services.PrintMessaging.Saga;

    public class PrintJobStateMessageMachine : PrintJobMassTransitStateMachine<PrintJobMessageState>
    {
        private IPrintJobService printJobService;
        private IMessageLogger logger;

        public PrintJobStateMessageMachine(IPrintJobService printJobService) : this(new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey), printJobService)
        {
        }

        public PrintJobStateMessageMachine(IMessageLogger logger, IPrintJobService printJobService) : this()
        {
            this.printJobService = printJobService;
            this.logger = logger;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:ParameterMustFollowComma", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        protected PrintJobStateMessageMachine()
        {
            this.InstanceState(x => x.CurrentState);

            this.Event(() => this.TransactionCommitted, x => x.CorrelateBy(state => $"{state.PolicyNumber}/{state.PolicyVersion}/{state.TransactionSequenceNumber}/{state.TransactionType}", context => $"{context.Message.PolicyNumber}/{context.Message.PolicyVersion}/{context.Message.TransactionSequenceNumber}/{context.Message.TransactionType}").SelectId(context => Guid.NewGuid()));

            this.Event(() => this.PrintJobMessageStateEvent, x => x.CorrelateById(context => context.Message.SagaId));

            this.DuringAny(this.Ignore(this.TransactionCommitted));

            this.Initially(this.When(this.TransactionCommitted, x => x.Data.Product != "HH")
                    .Then(context =>
                    {
                        context.Instance.PolicyNumber = context.Data.PolicyNumber;
                        context.Instance.PolicyVersion = context.Data.PolicyVersion;
                        context.Instance.TransactionSequenceNumber = context.Data.TransactionSequenceNumber;
                        context.Instance.AdditionalData = DateTime.Now.ToString("yyyyMMddhhmmss");
                        context.Instance.Step = 0;
                        context.Instance.StateMachineName = "PrintJobStateMessageMachine";
                        context.Instance.TransactionType = context.Data.TransactionType;
                    })
                    .TransitionTo(this.WorkerState));

            this.WhenEnter(this.WorkerState, binder => binder.Then(context => context.Publish<PrintJobMessageState, IPrintJobStateMessageEvent>(new
            {
                PolicyNumber = context.Instance.PolicyNumber,
                PolicyVersion = context.Instance.PolicyVersion,
                TransactionSequenceNumber = context.Instance.TransactionSequenceNumber,
                AdditionalData = context.Instance.AdditionalData,
                Step = context.Instance.Step,
                StateMachineName = context.Instance.StateMachineName,
                JobName = context.Instance.JobName,
                TransactionType = context.Instance.TransactionType,
                SagaId = context.Instance.CorrelationId
            }))
                .Then(context => context.Instance.Step++));

            this.WhenLeave(this.WorkerState, binder => binder.Then(context => this.Log<IPrintJobStateMessageEvent>($"Finished all Print Jobs for Transaction Committed {context.Instance.PolicyNumber}", null, this.logger)));

            this.During(this.WorkerState, this.When(this.PrintJobMessageStateEvent,
                context => context.Data.StateMachineName.Equals("PrintJobStateMessageMachine", StringComparison.CurrentCultureIgnoreCase))
                .Then(context => this.Log("Inside Worker state", context, this.logger))
                .If(context => !this.IsValidTransactionType(context.Data.TransactionType), binder =>
                    binder
                        .Then(context => this.Log("TransactionType Ignored", context, this.logger))
                        .Then(context => context.Data.Step = int.MaxValue))
                .If(context => context.Data.Step == 0, binder =>
                    binder
                    .Then(context => this.Log($"Auto ID Cards print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.AUTOIDCARDS, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log($"Auto ID Cards print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                //// We do not want to print RatingSummary when non renew commits. We therefore also need to increment the step as appropriate.
                .If(context => context.Data.Step == 1 && context.Data.TransactionType != Constants.TransactionType.NONRENEW, binder =>
                    binder
                    .Then(context => this.Log($"Rating Summary print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.RATINGSUMMARY, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log($"Rating Summary print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 1 && context.Data.TransactionType == Constants.TransactionType.NONRENEW, binder =>
                    binder
                    .Then(context => context.Data.Step++))
                .If(context => context.Data.Step == 2, binder =>
                   binder
                   .Then(context => this.Log($"Change summary print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                   .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.CHANGESUMMARY, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                   .Then(context => context.Data.Step++)
                   .Then(context => this.Log($"Change summary print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 3, binder =>
                   binder
                   .Then(context => this.Log($"DC3001 Extra Party print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                   .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.DC3001, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                   .Then(context => context.Data.Step++)
                   .Then(context => this.Log($"DC3001 Extra Party print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 4, binder =>
                   binder
                   .Then(context => this.Log($"DC3000 Extra Party print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                   .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.DC3000, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                   .Then(context => context.Data.Step++)
                   .Then(context => this.Log($"DC3000 Extra Party print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 5, binder =>
                   binder
                   .If(context => context.Data.TransactionType != Constants.TransactionType.NONRENEW, skipExtraPartyBinder =>
                    skipExtraPartyBinder
                        .Then(context => this.Log($"DC3002 Extra Party print job skipped for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                        .Then(context => context.Data.Step++))
                   .If(context => context.Data.TransactionType == Constants.TransactionType.NONRENEW, extraPartyBinder =>
                        extraPartyBinder
                       .Then(context => this.Log($"DC3002 Extra Party print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                       .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.DC3002, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                       .Then(context => context.Data.Step++)
                       .Then(context => this.Log($"DC3002 Extra Party print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger))))
                //// We do not want to print Application when non renew commits. We therefore also need to increment the step as appropriate.
                .If(context => context.Data.Step == 6 && context.Data.TransactionType != Constants.TransactionType.NONRENEW, binder =>
                   binder
                   .Then(context => this.Log($"Application print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                   .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.APPLICATION, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                   .Then(context => context.Data.Step++)
                   .Then(context => this.Log($"Application print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 6 && context.Data.TransactionType == Constants.TransactionType.NONRENEW, binder =>
                    binder
                    .Then(context => context.Data.Step++))
                .If(context => context.Data.Step == 7, binder =>
                    binder
                    .Then(context => this.Log($"Transaction print job started for transaction committed for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), Constants.PrintJobNames.TRANSACTIONPRINT, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log($"Transaction print job ended for transaction committed for {context.Data.PolicyNumber}", context, this.logger)))
                .Finalize());

            this.SetCompletedWhenFinalized();
        }

        public Event<ITransactionCommitted> TransactionCommitted { get; private set; }

        public Event<IPrintJobStateMessageEvent> PrintJobMessageStateEvent { get; private set; }

        public State WorkerState { get; private set; }

        internal bool IsValidTransactionType(string transactionType)
        {
            if (transactionType.Equals(Constants.TransactionType.NEWBUSINESSQUOTE, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.ENDORSEMENT, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.REINSTATEMENT, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.CANCEL, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.REWRITE, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.REISSUE, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.RENEWAL, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.NONRENEW, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
