// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BillingMessageState.cs" company="West Bend">
//   Copyright (c) 2018 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga.MessageStates
{
    using System;

    public class BillingMessageState : BaseState
    {
        public string ActivityTypeCode { get; set; }

        public string AccountNumber { get; set; }

        public string CustomerNumber { get; set; }

        public DateTime ActivityDateTime { get; set; }

        public DateTime PolicyEffectiveDate { get; set; }

        public DateTime PolicyExpirationDate { get; set; }

        public string IssueSystemCode { get; set; }

        public string FlatCancel { get; set; }

        public DateTime CancellationDate { get; set; }

        public decimal PCNAmountDue { get; set; }

        public string CancellationReason { get; set; }

        public string AdditionalData { get; set; }
    }
}
