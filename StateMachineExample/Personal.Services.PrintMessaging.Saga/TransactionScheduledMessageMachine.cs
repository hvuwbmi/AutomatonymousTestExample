// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransactionScheduledMessageMachine.cs" company="West Bend">
//   Copyright (c) 2020 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga
{
    using System;
    using System.Collections.Generic;
    using Automatonymous;
    using Personal.Contracts.CommonLog;
    using Personal.Service.Api.Logger;
    using Personal.Services.PrintMessaging.Saga.MessageStates;
    using Personal.Services.PrintMessaging.Saga.ServiceInterfaces;
    using WestBend.Personal.ServiceContracts.EventMessages;

    public class TransactionScheduledMessageMachine : PrintJobMassTransitStateMachine<TransactionScheduledMessageState>
    {
        private IPrintJobService printJobService;
        private IMessageLogger logger;

        public TransactionScheduledMessageMachine(IPrintJobService printJobService) : this(new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey), printJobService)
        {
        }

        public TransactionScheduledMessageMachine(IMessageLogger logger, IPrintJobService printJobService) : this()
        {
            this.printJobService = printJobService;
            this.logger = logger;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:ParameterMustFollowComma", Justification = "Is this really unreadable? Seems nice and fluent to me.")]
        protected TransactionScheduledMessageMachine()
        {
            List<string> validTransactionTypes = new List<string>
            {
                Constants.TransactionType.NONRENEW
            };

            this.InstanceState(x => x.CurrentState);

            this.Event(() => this.TransactionScheduled, x => x.CorrelateBy(state => $"{state.PolicyNumber}/{state.PolicyVersion}/{state.TransactionSequenceNumber}/{state.TransactionType}", context => $"{context.Message.PolicyNumber}/{context.Message.PolicyVersion}/{context.Message.TransactionSequenceNumber}/{context.Message.TransactionType}").SelectId(context => Guid.NewGuid()));

            this.Initially(
                When(this.TransactionScheduled, x => validTransactionTypes.Contains(x.Data.TransactionType))
                    .Then(context => AssignStateFromEvent(context.Instance, context.Data))
                    .Then(context => this.logger.Log(
                        new Log()
                        {
                            Messages = new List<LogMessage>()
                            {
                                new LogMessage()
                                {
                                    Category = Constants.Logging.CATEGORY,
                                    Severity = SeverityType.Information,
                                    Message = "NonRenewalActivityMessageMachine:message data",
                                    LogAttributes = new List<LogAttribute>()
                                    {
                                        new LogAttribute()
                                        {
                                            Key = "Event Name",
                                            Value = this.TransactionScheduled.Name,
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "data",
                                            Value = context.Instance.AdditionalData,
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "Current State",
                                            Value = context.Instance.CurrentState,
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "CorrelationId",
                                            Value = context.Instance.CorrelationId.ToString(),
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "ExpirationId",
                                            Value = context.Instance.ExpirationId.ToString(),
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "PolicyNumber",
                                            Value = context.Instance.PolicyNumber,
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "PolicyVersion",
                                            Value = context.Instance.PolicyVersion,
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "TransactionSequenceNumber",
                                            Value = context.Instance.TransactionSequenceNumber.ToString(),
                                        },
                                        new LogAttribute()
                                        {
                                            Key = "TransactionType",
                                            Value = context.Instance.TransactionType,
                                        }
                                    }
                                }
                            }
                        }))
                    .ThenAsync(context => this.printJobService.PostPrintJobRequest(context.Instance.PolicyNumber, context.Instance.PolicyVersion, context.Instance.TransactionSequenceNumber.ToString(), "_TransactionScheduled", string.Empty, Settings.ApimSubscriptionKey))
                    .Finalize());

            this.SetCompletedWhenFinalized();
        }

        public Event<ITransactionScheduled> TransactionScheduled { get; private set; }

        private static void AssignStateFromEvent(TransactionScheduledMessageState state, ITransactionScheduled scheduledEvent)
        {
            state.PolicyNumber = scheduledEvent.PolicyNumber;
            state.PolicyVersion = scheduledEvent.PolicyVersion;
            state.TransactionSequenceNumber = scheduledEvent.TransactionSequenceNumber;
            state.TransactionType = scheduledEvent.TransactionType;
            state.AdditionalData = DateTime.Now.ToString("yyyyMMddhhmmss");
        }
    }
}