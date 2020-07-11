// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransactionSubmittedMessageMachine.cs" company="West Bend">
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
    using ServiceInterfaces;
    using StateMachineMessageContracts.Messages.PrintMessaging;
    using WestBend.Personal.ServiceContracts.EventMessages;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:ParameterMustFollowComma", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
    public class TransactionSubmittedMessageMachine : PrintJobMassTransitStateMachine<TransactionSubmittedMessageState>
    {
        private IPrintJobService printJobService;
        private IMessageLogger logger;

        public TransactionSubmittedMessageMachine(IPrintJobService printJobService) : this(new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey), printJobService)
        {
        }

        public TransactionSubmittedMessageMachine(IMessageLogger logger, IPrintJobService printJobService) : this()
        {
            this.printJobService = printJobService;
            this.logger = logger;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:ParameterMustFollowComma", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        protected TransactionSubmittedMessageMachine()
        {
            this.InstanceState(x => x.CurrentState);

            this.Event(() => this.TransactionSubmitted, x => x.CorrelateBy(state => state.PolicyNumber.ToString(), context => context.Message.PolicyNumber.ToString()).SelectId(context => Guid.NewGuid()));
         
            this.Event(() => this.TransactionSubmittedMessageEvent, x => x.CorrelateById(context => context.Message.SagaId));

            this.DuringAny(this.Ignore(this.TransactionSubmitted));

            this.Initially(this.When(this.TransactionSubmitted)
                .Then(context =>
                {
                    context.Instance.PolicyNumber = context.Data.PolicyNumber;
                    context.Instance.PolicyVersion = context.Data.PolicyVersion;
                    context.Instance.TransactionSequenceNumber = context.Data.TransactionSequenceNumber;
                    context.Instance.UnderwritingAcceptabilityThreshold = context.Data.UnderwritingAcceptabilityThreshold;
                    context.Instance.AdditionalData = DateTime.Now.ToString("yyyyMMddhhmmss");
                    context.Instance.Step = 0;
                    context.Instance.StateMachineName = "TransactionSubmittedMessageMachine";
                    context.Instance.TransactionType = context.Data.TransactionType;
                })                
                .TransitionTo(this.WorkerState));

            this.WhenEnter(this.WorkerState, binder => binder.Then(context => context.Publish<TransactionSubmittedMessageState, ITransactionSubmittedMessageEvent>(new
            {
                PolicyNumber = context.Instance.PolicyNumber,
                PolicyVersion = context.Instance.PolicyVersion,
                TransactionSequenceNumber = context.Instance.TransactionSequenceNumber,
                UnderwritingAcceptabilityThreshold = context.Instance.UnderwritingAcceptabilityThreshold,
                AdditionalData = context.Instance.AdditionalData,
                Step = context.Instance.Step,
                StateMachineName = context.Instance.StateMachineName,
                JobName = context.Instance.JobName,
                TransactionType = context.Instance.TransactionType,
                SagaId = context.Instance.CorrelationId
            }))
            .Then(context => context.Instance.Step++));

            this.WhenLeave(this.WorkerState, binder => binder.Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Finished all Print Jobs for Transaction Submitted {context.Instance.PolicyNumber}", null, this.logger)));

            this.During(this.WorkerState, this.When(this.TransactionSubmittedMessageEvent, context => context.Data.StateMachineName.Equals("TransactionSubmittedMessageMachine", StringComparison.CurrentCultureIgnoreCase))
                .Then(context => this.Log("Inside Worker state", context, this.logger))
                .If(context => !this.IsValidTransactionType(context.Data.TransactionType), binder => 
                    binder
                        .Then(context => this.Log("TransactionType Ignored", context, this.logger))
                        .Then(context => context.Data.Step = int.MaxValue))
                .If(context => this.logger == null, binder => binder.Then(context => this.logger = new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey))
                .Then(context => this.logger.Log(new Log()
                {
                    Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Information,
                            Message = "RESET the logger was reset",
                        }
                    }
                }))).If(context => context.Data.Step == 0, binder =>
                    binder
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"AUS print job started for transaction submitted for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, Convert.ToString(context.Instance.TransactionSequenceNumber), Constants.PrintJobNames.AUS, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"AUS print job ended for transaction submitted for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 1, binder =>
                    binder
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Application print job started for transaction submitted for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, Convert.ToString(context.Instance.TransactionSequenceNumber), Constants.PrintJobNames.APPLICATION, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Application print job ended for transaction submitted for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 2 && !context.Data.UnderwritingAcceptabilityThreshold.Equals(Constants.LUCIThreshold.Green, StringComparison.InvariantCultureIgnoreCase), binder =>
                    binder
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Change summary print job started for transaction submitted for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, Convert.ToString(context.Instance.TransactionSequenceNumber), Constants.PrintJobNames.CHANGESUMMARY, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))                    
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Change summary print job ended for transaction submitted for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 3 && !context.Data.UnderwritingAcceptabilityThreshold.Equals(Constants.LUCIThreshold.Green, StringComparison.InvariantCultureIgnoreCase), binder =>
                    binder
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"DC3001 Extra Party print job started for transaction submitted for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, Convert.ToString(context.Instance.TransactionSequenceNumber), Constants.PrintJobNames.DC3001, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))                    
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"DC3001 Extra Party print job ended for transaction submitted for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 4 && !context.Data.UnderwritingAcceptabilityThreshold.Equals(Constants.LUCIThreshold.Green, StringComparison.InvariantCultureIgnoreCase), binder =>
                    binder
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"DC3000 Extra Party print job started for transaction submitted for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, Convert.ToString(context.Instance.TransactionSequenceNumber), Constants.PrintJobNames.DC3000, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))                    
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"DC3000 Extra Party print job ended for transaction submitted for {context.Data.PolicyNumber}", context, this.logger)))
                .If(context => context.Data.Step == 5, binder =>
                    binder
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Auto ID Cards print job started for transaction submitted for {context.Data.PolicyNumber}", context, this.logger))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, Convert.ToString(context.Instance.TransactionSequenceNumber), Constants.PrintJobNames.AUTOIDCARDS, context.Instance.AdditionalData, Settings.ApimSubscriptionKey))                    
                    .Then(context => context.Data.Step++)
                    .Then(context => this.Log<ITransactionSubmittedMessageEvent>($"Auto ID Cards print job ended for transaction submitted for {context.Data.PolicyNumber}", context, this.logger)))
                .Finalize());

            this.SetCompletedWhenFinalized();
        }

        public Event<ITransactionSubmitted> TransactionSubmitted { get; private set; }

        public Event<ITransactionSubmittedMessageEvent> TransactionSubmittedMessageEvent { get; private set; }

        public State WorkerState { get; private set; }

        private bool IsValidTransactionType(string transactionType)
        {
            if (transactionType.Equals(Constants.TransactionType.NEWBUSINESSQUOTE, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (transactionType.Equals(Constants.TransactionType.ENDORSEMENT, StringComparison.InvariantCultureIgnoreCase))
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

            return false;
        }
    }
}