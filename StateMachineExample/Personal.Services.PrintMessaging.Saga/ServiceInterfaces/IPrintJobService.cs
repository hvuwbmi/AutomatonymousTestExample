// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPrintJobService.cs" company="West Bend">
//   Copyright (c) 2018 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga.ServiceInterfaces
{
    using System.Threading.Tasks;
    using Refit;

    public interface IPrintJobService
    {
        [Get("/Personal/Policy/Store/Print/PolicyNumber/{policyNumber}/{policyVersion}/{transactionSequenceNumber}/jobName/{jobName}?additionalData={additionalData}")]
        Task<string> PostPrintJobRequest(string policyNumber, string policyVersion, string transactionSequenceNumber, string jobName, string additionalData, [Header("Ocp-Apim-Subscription-Key")] string subscriptionKey);

        [Get("/Personal/Policy/Store/transact/{policyNumber}")]
        Task<string> GetPolicyTransactions(string policyNumber, [Header("Ocp-Apim-Subscription-Key")] string subscriptionKey);
    }
}
