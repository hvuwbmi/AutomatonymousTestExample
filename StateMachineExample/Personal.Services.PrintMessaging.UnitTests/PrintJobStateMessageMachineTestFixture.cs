// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintJobStateMessageMachineTestFixture.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Personal.Service.Api.Logger;
    using Personal.Services.PrintMessaging.Saga;
    using Personal.Services.PrintMessaging.Saga.ServiceInterfaces;
    using Rhino.Mocks;
    using Should;

    [TestClass]
    public class PrintJobStateMessageMachineTestFixture
    {
        [TestMethod]
        [DataRow("NBQ")]
        [DataRow("PCH")]
        [DataRow("REI")]
        [DataRow("XLC")]
        [DataRow("REW")]
        [DataRow("RIX")]
        [DataRow("RWL")]
        public void Test_IsValidTransactionType_Success(string activityTypeCode)
        {
            // arrange
            var printService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var machine = new PrintJobStateMessageMachine(logger, printService);

            // act
            var result = machine.IsValidTransactionType(activityTypeCode);

            // assert
            result.ShouldBeTrue();
        }

        [TestMethod]
        [DataRow("INVALID")]
        public void Test_IsValidTransactionType_Failure(string activityTypeCode)
        {
            // arrange
            var printService = MockRepository.GenerateMock<IPrintJobService>();
            var logger = MockRepository.GenerateMock<IMessageLogger>();
            var machine = new PrintJobStateMessageMachine(logger, printService);

            // act
            var result = machine.IsValidTransactionType(activityTypeCode);

            // assert
            result.ShouldBeFalse();
        }
    }
}
