// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TransactionSubmittedMessageState.cs" company="West Bend">
//   Copyright (c) 2018 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga.MessageStates
{
    public class TransactionSubmittedMessageState : BaseState
    {
        public string AdditionalData { get; set; }

        public string TransactionType { get; set; }

        public string JobName { get; set; }
    }
}
