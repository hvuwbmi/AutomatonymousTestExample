// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga
{
    public static class Constants
    {
        public static class Logging
        {
            public const string CATEGORY = "PL.PrintMessageService";
        }

        public static class PrintJobNames
        {
            public const string AUS = "_AUSReferrals";
            public const string AUTOIDCARDS = "_AutoIDCards";
            public const string ISSUENEWINSATN = "_IssueNew_INSATN";
            public const string APPLICATION = "_Application";
            public const string TRANSACTIONPRINT = "_TransactionPrint";
            public const string CHANGESUMMARY = "_ChangeSummary";
            public const string RATINGSUMMARY = "_RatingSummary";
            public const string CANCELPENDING = "_CANCELPENDING";
            public const string RESCINDCANCELPENDING = "_RescindCancelPending";
            public const string DC3001 = "_DC3001"; ////Extra Party Termination Form
            public const string DC3000 = "_DC3000"; ////Extra Party Inclusion Form
            public const string DC3002 = "_DC3002"; ////Extra Party NonRenew Form
        }

        public static class Service
        {
            public const string NAME = "PL.PrintMessaging";
        }

        public static class LUCIThreshold
        {
            public const string Green = "Green";
            public const string Yellow = "Yellow";
            public const string Red = "Red";
        }

        public static class TransactionType
        {
            public const string NEWBUSINESSQUOTE = "NBQ";
            public const string ENDORSEMENT = "PCH";
            public const string CANCEL = "XLC";
            public const string REINSTATEMENT = "REI";
            public const string PENDINGCANCEL = "PCN";
            public const string REWRITE = "REW";
            public const string REISSUE = "RIX";
            public const string RENEWAL = "RWL";
            public const string NONRENEW = "RWX";
        }

        public static class IssueSystemCode
        {
            public const string DUCKCOMMERCIAL = "DCT";
            public const string DUCKPERSONAL = "DCTP";
        }

        public static class BillingActivityTypeCodes
        {
            public const string RESCINDPCN = "RESCINDPCN";
        }
    }
}