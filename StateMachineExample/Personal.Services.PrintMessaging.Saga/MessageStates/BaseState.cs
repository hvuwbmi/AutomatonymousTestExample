// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseState.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga.MessageStates
{
    using System;
    using Automatonymous;

    public class BaseState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; }

        public string StateMachineName { get; set; }

        public int Step { get; set; }

        public string PolicyNumber { get; set; }

        public string PolicyVersion { get; set; }

        public int TransactionSequenceNumber { get; set; }

        public string UnderwritingAcceptabilityThreshold { get; set; }
    }
}