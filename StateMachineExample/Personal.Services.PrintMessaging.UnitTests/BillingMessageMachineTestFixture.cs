// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BillingMessageMachineTestFixture.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.UnitTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using MassTransit.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Personal.Contracts.CommonLog;
    using Personal.Service.Api.Logger;
    using Personal.Services.PrintMessaging.Saga;
    using Personal.Services.PrintMessaging.Saga.MessageStates;
    using Personal.Services.PrintMessaging.Saga.ServiceInterfaces;
    using Rhino.Mocks;
    using Should;
    using WestBend.Billing.ServiceContracts.EventMessages;

    [TestClass]
    public class BillingMessageMachineTestFixture
    {
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:SplitParametersMustStartOnLineAfterDeclaration", Justification = "This is readable.")]
        private readonly XDocument transactionStatusValidXml = new XDocument(
            new XElement("transactions",
                new XElement("transaction",
                    new XElement("PolicyVersion", "00"),
                    new XElement("Status", "Committed"),
                    new XElement("TransactionCounter", "1"))));

        [TestMethod]
        public void Test_BillingMessageMachine_Constructor_Successful()
        {
            // arrange
            var printJobService = MockRepository.GenerateMock<IPrintJobService>();

            // act
            var machine = new BillingMessageMachine(printJobService);

            // assert
            machine.ShouldNotBeNull();
        }

        [TestMethod]
        public void Test_BillingMessageMachine_Constructor2arg_Successful()
        {
            // arrange
            var printJobService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();

            // act
            var machine = new BillingMessageMachine(printJobService, logger);

            // assert
            machine.ShouldNotBeNull();
        }

        [DataRow(Constants.PrintJobNames.CANCELPENDING, "INVALID", Constants.IssueSystemCode.DUCKPERSONAL)]
        [DataRow(Constants.PrintJobNames.CANCELPENDING, Constants.TransactionType.PENDINGCANCEL, "INVALID")]
        [TestMethod]
        public void Test_BillingMessageMachine_SendMessage_InvalidCode(string printJobName, string activityTypeCode, string issueSystemCode)
        {
            // arrange
            var printJobService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var harness = new InMemoryTestHarness();
            var machine = new BillingMessageMachine(printJobService, logger);
            var saga = harness.StateMachineSaga<BillingMessageState, BillingMessageMachine>(machine);

            var accountActivity = new AccountActivity
            {
                ActivityTypeCode = activityTypeCode,
                Policy = new Policy
                {
                    PolicyNumber = "A123456",
                    Mod = "00",
                    IssueSystemCode = issueSystemCode
                }
            };

            // act
            harness.Start().Wait();

            harness.Bus.Publish<IAccountActivity>(accountActivity).Wait();

            // wait for the publish to do its thing, the Wait() above doesn't guarantee much.
            var receivedMessage = harness.Consumed.Select<IAccountActivity>().FirstOrDefault();

            // assert
            var message = logger.GetArgumentsForCallsMadeOn<IMessageLogger>(x => x.Log(Arg<Log>.Is.Anything, Arg<string>.Is.Null, Arg<string>.Is.Null, Arg<string>.Is.Null));
            Log argumentLog = (Log)message[0][0];
            LogMessage logMessage = argumentLog.Messages[0];
            Assert.AreEqual(logMessage.Category, Constants.Logging.CATEGORY);
            Assert.AreEqual(logMessage.Severity, SeverityType.Information);

            printJobService.AssertWasNotCalled(p => p.GetPolicyTransactions(Arg<string>.Is.Anything, Arg<string>.Is.Anything));
            printJobService.VerifyAllExpectations();
            receivedMessage.Exception.ShouldBeNull();
            harness.Consumed.Count().ShouldEqual(1);
            harness.Published.Count().ShouldEqual(1);
            harness.Sent.Count().ShouldEqual(0);
            saga.Consumed.Count().ShouldEqual(1);

            // cleanup
            harness.Stop().Wait();
        }

        [DataRow(Constants.PrintJobNames.CANCELPENDING, Constants.TransactionType.PENDINGCANCEL, Constants.IssueSystemCode.DUCKPERSONAL)]
        [DataRow(Constants.PrintJobNames.RESCINDCANCELPENDING, Constants.BillingActivityTypeCodes.RESCINDPCN, Constants.IssueSystemCode.DUCKPERSONAL)]
        [TestMethod]
        public void Test_BillingMessageMachine_SendMessage(string printJobName, string activityTypeCode, string issueSystemCode)
        {
            // arrange
            var printJobService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var harness = new InMemoryTestHarness();
            var machine = new BillingMessageMachine(printJobService, logger);
            var saga = harness.StateMachineSaga<BillingMessageState, BillingMessageMachine>(machine);

            printJobService
                .Expect(p => p.GetPolicyTransactions("A123456", "58f31946b12b441892cd798973aab87e"))
                .Return(Task.FromResult<string>(this.transactionStatusValidXml.ToString()));

            printJobService
                .Expect(p => p.PostPrintJobRequest(Arg.Is("A123456"), Arg.Is("00"), Arg.Is("1"), Arg.Is(printJobName), Arg<string>.Is.Anything, Arg.Is("58f31946b12b441892cd798973aab87e")))
                .Return(Task.FromResult<string>("printed!"));

            var accountActivity = new AccountActivity
            {
                ActivityTypeCode = activityTypeCode,
                Policy = new Policy
                {
                    PolicyNumber = "A123456",
                    Mod = "00",
                    IssueSystemCode = issueSystemCode
                }
            };

            // act
            harness.Start().Wait();

            harness.Bus.Publish<IAccountActivity>(accountActivity).Wait();

            // wait for the publish to do its thing, the Wait() above doesn't guarantee much.
            var receivedMessage = harness.Consumed.Select<IAccountActivity>().FirstOrDefault();

            // assert
            printJobService.VerifyAllExpectations();
            logger.VerifyAllExpectations();

            receivedMessage.Exception.ShouldBeNull();
            harness.Consumed.Count().ShouldEqual(1);
            harness.Published.Count().ShouldEqual(1);
            harness.Sent.Count().ShouldEqual(0);
            saga.Consumed.Count().ShouldEqual(1);

            // cleanup
            harness.Stop().Wait();
        }

        [TestMethod]
        [DataRow("DCTP")]
        public void Test_IsValidIssueSystemCode_Success(string issueSystemCode)
        {
            // arrange
            var printService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var machine = new BillingMessageMachine(printService, logger);

            // act
            var result = machine.IsValidIssueSystemCode(issueSystemCode, "A123456", logger);

            // assert
            result.ShouldBeTrue();
        }

        [TestMethod]
        [DataRow("DCT")]
        [DataRow("PMS")]
        public void Test_IsValidIssueSystemCode_Failure(string issueSystemCode)
        {
            // arrange
            var printService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var machine = new BillingMessageMachine(printService, logger);

            // act
            var result = machine.IsValidIssueSystemCode(issueSystemCode, "A123456", logger);

            // assert
            result.ShouldBeFalse();
        }

        [TestMethod]
        [DataRow("PCN")]
        [DataRow("RESCINDPCN")]
        public void Test_IsValidActivityType_Success(string activityTypeCode)
        {
            // arrange
            var printService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var machine = new BillingMessageMachine(printService, logger);

            // act
            var result = machine.IsValidActivityType(activityTypeCode, "A123456", logger);

            // assert
            result.ShouldBeTrue();
        }

        [TestMethod]
        [DataRow("INVALID")]
        public void Test_IsValidActivityType_Failure(string activityTypeCode)
        {
            // arrange
            var printService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var machine = new BillingMessageMachine(printService, logger);

            // act
            var result = machine.IsValidActivityType(activityTypeCode, "A123456", logger);

            // assert
            result.ShouldBeFalse();
        }

        public class AccountActivity : IAccountActivity
        {
            public string ActivityTypeCode { get; set; }

            public string AccountNumber { get; set; }

            public string CustomerNumber { get; set; }

            public string ActivityDateTime { get; set; }

            public IPolicy Policy { get; set; }

            public IExtendedData ExtendedData { get; set; }
        }

        public class Policy : IPolicy
        {
            public string PolicyNumber { get; set; }

            public string EffectiveDate { get; set; }

            public string ExpirationDate { get; set; }

            public string Mod { get; set; }

            public string IssueSystemCode { get; set; }
        }
    }
}
