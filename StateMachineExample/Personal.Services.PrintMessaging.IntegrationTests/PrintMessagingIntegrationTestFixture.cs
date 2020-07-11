// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintMessagingIntegrationTestFixture.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.IntegrationTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Personal.Services.PrintMessaging.Saga.ServiceInterfaces;
    using Refit;
    using Should;

    [TestClass]
    public class PrintMessagingIntegrationTestFixture
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task Test_PrintJobRequest_SuccessfulAsync()
        {
            var printJobService = RestService.For<IPrintJobService>("https://dev-api.wbmi.com");
            var result = await printJobService.PostPrintJobRequest("A552010", "00", "1", "_AutoIDCards", DateTime.Now.ToString("yyyyMMddhhmmss"), "58f31946b12b441892cd798973aab87e");

            using (var sr = new StreamReader("data/test_printjobrequest_successfulasync.txt"))
            {
                var correctval = await sr.ReadToEndAsync();
                result.ShouldEqual(correctval);
            }
        }
    }
}
