// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransactionScheduledMessageState.cs" company="West Bend">
//   Copyright (c) 2020 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga.MessageStates
{
    using System;
    using Automatonymous;

    public class TransactionScheduledMessageState : SagaStateMachineInstance
    {
        public string CurrentState { get; set; }

        public string PolicyNumber { get; set; }

        public string PolicyVersion { get; set; }

        public int TransactionSequenceNumber { get; set; }

        public string TransactionType { get; set; }

        public string EffectiveDate { get; set; }

        public string Reason { get; set; }

        public string JobName { get; set; }

        public string AdditionalData { get; set; }

        public Guid CorrelationId { get; set; }

        public Guid? ExpirationId { get; set; }
    }
}
