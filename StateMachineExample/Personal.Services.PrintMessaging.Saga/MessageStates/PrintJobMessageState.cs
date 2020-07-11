// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintJobMessageState.cs" company="West Bend">
//   Copyright (c) 2018 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WestBend.Services.PrintMessaging.Saga
{
    using System;
    using global::Personal.Services.PrintMessaging.Saga.MessageStates;

    public class PrintJobMessageState : BaseState
    {
        public string JobName { get; set; }

        public string AdditionalData { get; set; }

        public Guid? ExpirationId { get; set; }

        public string TransactionType { get; set; }
    }
}
