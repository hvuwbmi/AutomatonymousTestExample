// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintControllerTestFixture.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.UnitTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Personal.Service.Api.Logger;
    using Personal.Services.PrintMessaging.Saga;
    using Personal.Services.PrintMessaging.Saga.Controllers;
    using Personal.Services.PrintMessaging.Saga.MessageStates;
    using Personal.Services.PrintMessaging.Saga.ServiceInterfaces;
    using Should;

    [TestClass]
    public class PrintControllerTestFixture
    {
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "This is readable.")]
        private readonly XDocument transactionStatusValidXml = new XDocument(
            new XElement("transactions",
                new XElement("transaction",
                    new XElement("PolicyVersion", "00"),
                    new XElement("Status", "Committed"),
                    new XElement("TransactionCounter", "1"))));

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "This is readable.")]
        private readonly XDocument transactionStatusMultipleEndorsements = new XDocument(
            new XElement("transactions",
                new XElement("transaction",
                    new XElement("PolicyVersion", "00"),
                    new XElement("Status", "Committed"),
                    new XElement("TransactionCounter", "1")),
                new XElement("transaction",
                    new XElement("PolicyVersion", "00"),
                    new XElement("Status", "Committed"),
                    new XElement("TransactionCounter", "3")),
                new XElement("transaction",
                    new XElement("PolicyVersion", "00"),
                    new XElement("Status", "Committed"),
                    new XElement("TransactionCounter", "2")),
                new XElement("transaction",
                    new XElement("PolicyVersion", "00"),
                    new XElement("Status", "Committed"),
                    new XElement("TransactionCounter", "2"))));

        [DataRow(Constants.PrintJobNames.CANCELPENDING, Constants.TransactionType.PENDINGCANCEL)]
        [DataRow(Constants.PrintJobNames.RESCINDCANCELPENDING, Constants.BillingActivityTypeCodes.RESCINDPCN)]
        [TestMethod]
        public void Test_PrintController_QueueCancelPendingPrint_Successful(string printJobName, string activityTypeCode)
        {
            // arrange
            var mockPrintJobService = new Mock<IPrintJobService>();
            var mockLogger = new Mock<IMessageLogger>();
            mockPrintJobService
                .Setup(p => p.PostPrintJobRequest("A123456", "00", "1", printJobName, "addlData", "58f31946b12b441892cd798973aab87e"))
                .Returns(Task.FromResult<string>("result"));
            mockPrintJobService
                .Setup(p => p.GetPolicyTransactions("A123456", "58f31946b12b441892cd798973aab87e"))
                .Returns(Task.FromResult<string>(this.transactionStatusValidXml.ToString()));
            var printController = new PrintController(mockPrintJobService.Object, mockLogger.Object);
            var messageState = new BillingMessageState
            {
                ActivityTypeCode = activityTypeCode,
                PolicyNumber = "A123456",
                PolicyVersion = "00",
                TransactionSequenceNumber = 1,
                AdditionalData = "addlData"
            };

            // act
            string result = printController.QueueCancelPendingPrint(messageState).Result;

            // assert
            mockPrintJobService.VerifyAll();
            mockLogger.VerifyAll();

            result.ShouldNotBeNull();
            result.ShouldEqual("result");
        }

        [DataRow(Constants.PrintJobNames.CANCELPENDING, Constants.TransactionType.PENDINGCANCEL)]
        [DataRow(Constants.PrintJobNames.RESCINDCANCELPENDING, Constants.BillingActivityTypeCodes.RESCINDPCN)]
        [TestMethod]
        public void Test_PrintController_QueueCancelPendingPrint_MultipleEndorsements_Successful(string printJobName, string activityTypeCode)
        {
            // arrange
            var mockPrintJobService = new Mock<IPrintJobService>();
            var mockLogger = new Mock<IMessageLogger>();
            mockPrintJobService
                .Setup(p => p.PostPrintJobRequest("A123456", "00", "3", printJobName, "addlData", "58f31946b12b441892cd798973aab87e"))
                .Returns(Task.FromResult<string>("result"));
            mockPrintJobService
                .Setup(p => p.GetPolicyTransactions("A123456", "58f31946b12b441892cd798973aab87e"))
                .Returns(Task.FromResult<string>(this.transactionStatusMultipleEndorsements.ToString()));
            var printController = new PrintController(mockPrintJobService.Object, mockLogger.Object);
            var messageState = new BillingMessageState
            {
                ActivityTypeCode = activityTypeCode,
                PolicyNumber = "A123456",
                PolicyVersion = "00",
                TransactionSequenceNumber = 1,
                AdditionalData = "addlData"
            };

            // act
            string result = printController.QueueCancelPendingPrint(messageState).Result;

            // assert
            mockPrintJobService.VerifyAll();
            mockLogger.VerifyAll();

            result.ShouldNotBeNull();
            result.ShouldEqual("result");
        }

        [TestMethod]
        public void Test_PrintController_QueueCancelPendingPrint_InvalidXml()
        {
            // arrange
            bool caughtException = false;
            var mockPrintJobService = new Mock<IPrintJobService>();
            var mockLogger = new Mock<IMessageLogger>();
            mockPrintJobService
                .Setup(p => p.GetPolicyTransactions("A123456", "58f31946b12b441892cd798973aab87e"))
                .Returns(Task.FromResult<string>("invalid xml"));
            var printController = new PrintController(mockPrintJobService.Object, mockLogger.Object);
            var messageState = new BillingMessageState
            {
                PolicyNumber = "A123456",
                PolicyVersion = "00",
                TransactionSequenceNumber = 1,
                AdditionalData = "addlData"
            };

            // act
            try
            {
                string result = printController.QueueCancelPendingPrint(messageState).Result;
            }
            catch (XmlException)
            {
                caughtException = true;
            }

            // assert
            caughtException.ShouldBeTrue();
            mockPrintJobService.VerifyAll();
            mockLogger.VerifyAll();
        }
    }
}
