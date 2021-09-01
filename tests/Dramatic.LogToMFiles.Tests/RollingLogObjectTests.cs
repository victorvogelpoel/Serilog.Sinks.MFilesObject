using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dramatic.LogToMFiles.Application;
using FluentAssertions;
using MFilesAPI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Dramatic.LogToMFiles.Tests
{
    public static class StringExtensions
    {
        public static string Repeat(this string s, int n) => new StringBuilder(s.Length * n).Insert(0, s, n).ToString();

        public static string RepeatWithLineCounter(this string s, int n, int lineCounter=1)
        {
            if (lineCounter == 0) { lineCounter = 1; }

            var sb = new StringBuilder(s.Length * n + 4 * n);
            for (int i=0; i < n; i++) { sb.Append($"{lineCounter++:0000}:{s}"); }

            return sb.ToString();
        }

    }



    public class RollingLogObjectTests
    {
        // source: https://stackoverflow.com/questions/155436/unit-test-naming-best-practices
        // source: http://haacked.com/archive/2012/01/02/structuring-unit-tests.aspx/

        [TestClass]
        public class ConstructionTests
        {

            [DataTestMethod]
            [DataRow("Log-")]
            [DataRow("This is a log obj name prefix of 50 char or less-")]
            public void WhenConstructorArgumentAreInValidRange_ExpectNoExceptions(string logObjectNamePrefix)
            {
                // ASSIGN
                var logVaultMock  = new Mock<ILogObjectVault>();

                // ACT
                Action act = () => new RollingLogObject(logVaultMock.Object, logObjectNamePrefix);

                // ASSERT
                act.Should().NotThrow();
            }



            // ------------------------------------------------------------------------------------------------------------------------
            // Failure tests

            [TestMethod]
            public void WhenLogVaultArgumentIsNull_ExpectArgumentNullException()
            {
                // ASSIGN
                ILogObjectVault logVault  = null;

                // ACT
                Action act = () => new RollingLogObject(logVault);

                // ASSERT
                act.Should().Throw<ArgumentNullException>();
            }


            [DataTestMethod]
            [DataRow(null)]
            [DataRow("")]
            public void WhenLogObjectNamePrefixArgumentIsNull_ExpectArgumentNullException(string logObjectNamePrefix)
            {
                // ASSIGN
                var logVaultMock  = new Mock<ILogObjectVault>();

                // ACT
                Action act = () => new RollingLogObject(logVaultMock.Object, logObjectNamePrefix);

                // ASSERT
                act.Should().Throw<ArgumentNullException>();
            }


            [DataTestMethod]
            [DataRow("This is a too long prefix name for the Log object and should be 50 characters or less-")]
            [DataRow("Some very long logging prefix, with extra information and then some more-")]
            public void WhenLogObjectNamePrefixArgumentIsLargerThan50Characters_ExpectArgumentOutofRangeException(string logObjectNamePrefix)
            {
                // ASSIGN
                var logVaultMock  = new Mock<ILogObjectVault>();

                // ACT
                Action act = () => new RollingLogObject(logVaultMock.Object, logObjectNamePrefix);

                // ASSERT
                act.Should().Throw<ArgumentOutOfRangeException>();
            }
        }



        [TestClass]
        public class SaveLogMessageTests
        {
            [TestMethod]
            public void WhenLoggingStructureIsNotPresent_ExpectNoResult()
            {
                // ASSIGN
                var logObjectNamePrefix             = "Log-";
                var mfilesLogMessageCharacterLimit  = 10000;
                var minCharsForRollover             = 15;
                var logMessage                      = "Some message";

                var logVaultMock                    = new Mock<ILogObjectVault>();
                logVaultMock.Setup(m => m.IsLogObjectStructurePresent()).Returns(false);


                // ACT
                var rollingLogObject = new RollingLogObject(logVaultMock.Object, logObjectNamePrefix);
                rollingLogObject.SaveLogMessage(logMessage);

                // ASSERT
                logVaultMock.Verify((m => m.IsLogObjectStructurePresent()), Times.Once);
                logVaultMock.VerifyNoOtherCalls();
            }



            [TestMethod]
            public void WhenLogMessageIsEmpty_ExpectNoWriteToExistingNorNewLogObject()
            {
                // ASSIGN
                var logObjectNamePrefix             = "Log-";
                var mfilesLogMessageCharacterLimit  = 10000;
                var minCharsForRollover             = 15;
                var logMessage                      = ""; // Empty
                var date                            = DateTime.Today;

                var logVaultMock                    = new Mock<ILogObjectVault>();
                logVaultMock.Setup(b => b.IsLogObjectStructurePresent()).Returns(true);
                logVaultMock.Setup(b => b.SearchLogObjects(logObjectNamePrefix, date)).Returns(new List<MFilesAPI.ObjVer>());

                // ACT
                var rollingLogObject = new RollingLogObject(logVaultMock.Object, logObjectNamePrefix);
                rollingLogObject.SaveLogMessage(logMessage);


                // ASSERT
                logVaultMock.Verify(m => m.IsLogObjectStructurePresent(), Times.Once);
                logVaultMock.VerifyNoOtherCalls();
            }

            [TestMethod]
            public void WhenTodaysLogObjectDoesNotExist_ExpectANewLogObjectCreated()
            {
                // ASSIGN
                var logObjectNamePrefix             = "Log-";
                var mfilesLogMessageCharacterLimit  = 10000;
                var minCharsForRollover             = 15;
                var logMessage                      = "Some message";
                var date                            = DateTime.Today;

                var logVaultMock                    = new Mock<ILogObjectVault>();
                logVaultMock.Setup(b => b.IsLogObjectStructurePresent()).Returns(true);
                logVaultMock.Setup(b => b.SearchLogObjects(logObjectNamePrefix, date)).Returns(new List<MFilesAPI.ObjVer>());

                var expectedLogObjectOrdinal        = 1;
                var expectedBatchedLogMessage       = $"{logMessage}{Environment.NewLine}";

                // ACT
                var rollingLogObject = new RollingLogObject(logVaultMock.Object, logObjectNamePrefix);
                rollingLogObject.SaveLogMessage(logMessage);


                // ASSERT
                logVaultMock.Verify(m => m.IsLogObjectStructurePresent(), Times.Once);
                logVaultMock.Verify(m => m.SearchLogObjects(logObjectNamePrefix, date), Times.Once);
                logVaultMock.Verify(m => m.WriteLogMessageToExistingLogObject(It.IsAny<ObjVer>(), logMessage), Times.Never);
                logVaultMock.Verify(m => m.WriteLogMessageToNewLogObject(logObjectNamePrefix, date, expectedLogObjectOrdinal, expectedBatchedLogMessage), Times.Once);

            }


            [TestClass]
            public class Stubbed
            {
                private ObjVer CreateObjVerWithID(int ID)
                {
                    var objVer = new ObjVerClass
                    {
                        ID = ID
                    };

                    return objVer;
                }


                [TestMethod]
                public void WhenTodaysLogObjectDoesNotExistAndLogMessageFitsInOneObject_ExpectOneNewLogObjectCreated()
                {
                    var logObjectNamePrefix             = "Log-";
                    var logMessage                      = "Some message";   // Smaller than 10000 characters
                    var logVaultStub                    = new LogVaultStub();

                    var expectedObjectNameOrTitle       = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}";
                    var expectedObjectLogMessage        = $"{logMessage}{Environment.NewLine}";

                    // ACT
                    var rollingLogObject = new RollingLogObject(logVaultStub, logObjectNamePrefix);
                    rollingLogObject.SaveLogMessage(logMessage);

                    // ASSERT
                    logVaultStub.LogObjects.Should().NotBeNull();
                    logVaultStub.LogObjects.Count.Should().Be(1);
                    logVaultStub.LogObjects[0].NameOrTitle.Should().Be(expectedObjectNameOrTitle);
                    logVaultStub.LogObjects[0].LogMessage.Should().Be(expectedObjectLogMessage);
                }


                [TestMethod]
                public void WhenTodaysLogObjectDoesNotExistAndLogMessageDoesNotFitInOneObject_ExpectTwoNewLogObjectsCreated()
                {
                    var logObjectNamePrefix             = "Log-";
                    var logLine                         = "This line is a hundred characters in size 0123456789012345678901234567890123456789012345678901234\r\n";
                    var logMessage                      = logLine.RepeatWithLineCounter(100);
                    var logVaultStub                    = new LogVaultStub();

                    var expectedObject1NameOrTitle      = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}";
                    var expectedObject1LogMessage       = logLine.RepeatWithLineCounter(96); // 96 repeats are stored in the first LogObject
                    var expectedObject2NameOrTitle      = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd} (2)";
                    var expectedObject2LogMessage       = logLine.RepeatWithLineCounter(4, lineCounter: 97);;

                    // ACT
                    var rollingLogObject = new RollingLogObject(logVaultStub, logObjectNamePrefix);
                    rollingLogObject.SaveLogMessage(logMessage);

                    // ASSERT
                    logVaultStub.LogObjects.Should().NotBeNull();
                    logVaultStub.LogObjects.Count.Should().Be(2);
                    logVaultStub.LogObjects[0].NameOrTitle.Should().Be(expectedObject1NameOrTitle);
                    logVaultStub.LogObjects[0].LogMessage.Should().Be(expectedObject1LogMessage);
                    logVaultStub.LogObjects[1].NameOrTitle.Should().Be(expectedObject2NameOrTitle);
                    logVaultStub.LogObjects[1].LogMessage.Should().Be(expectedObject2LogMessage);

                }

                [TestMethod]
                public void WhenPreviousDaysLogObjectsExists_ExpectNewTodaysLogObjectCreated()
                {
                    var logObjectNamePrefix             = "Log-";
                    var logMessage                      = "Some message";
                    var logVaultStub                    = new LogVaultStub();

                    var existingPreviousObjectName      = $"{logObjectNamePrefix}{DateTime.Today.AddDays(-2):yyyy-MM-dd}";
                    var existingPreviousLogMessage      = $"Existing message for {DateTime.Today.AddDays(-2)}\r\n";

                    logVaultStub.LogObjects.Add(new LogObjectStub
                    {
                        NameOrTitle = existingPreviousObjectName,
                        LogMessage  = existingPreviousLogMessage,
                        ObjVer      = CreateObjVerWithID(1)
                    });

                    var expectedTodaysObjectNameOrTitle = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}";
                    var expectedTodaysObjectLogMessage  = $"{logMessage}{Environment.NewLine}";

                    // ACT
                    var rollingLogObject = new RollingLogObject(logVaultStub, logObjectNamePrefix);
                    rollingLogObject.SaveLogMessage(logMessage);

                    // ASSERT
                    logVaultStub.LogObjects.Should().NotBeNull();
                    logVaultStub.LogObjects.Count.Should().Be(2);

                    // Assert the previous day logobject as unchanged
                    logVaultStub.LogObjects[0].NameOrTitle.Should().Be(existingPreviousObjectName);
                    logVaultStub.LogObjects[0].LogMessage.Should().Be(existingPreviousLogMessage);

                    // assert the newly created Todays LogObject
                    logVaultStub.LogObjects[1].NameOrTitle.Should().Be(expectedTodaysObjectNameOrTitle);
                    logVaultStub.LogObjects[1].LogMessage.Should().Be(expectedTodaysObjectLogMessage);

                }

                [TestMethod]
                public void WhenTodaysLogObjectExistsAndLogMessageFits_ExpectLogMessageAppendedToTodaysLogObject()
                {
                    var logObjectNamePrefix             = "Log-";
                    var logMessage                      = "Some message";
                    var logVaultStub                    = new LogVaultStub();

                    logVaultStub.LogObjects.Add(new LogObjectStub
                    {
                        NameOrTitle = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}",
                        LogMessage  = "Existing message\r\n",
                        ObjVer      = CreateObjVerWithID(1)
                    });

                    var expectedObjectNameOrTitle       = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}";
                    var expectedObjectLogMessage        = $"Existing message\r\n{logMessage}{Environment.NewLine}";

                    // ACT
                    var rollingLogObject = new RollingLogObject(logVaultStub, logObjectNamePrefix);
                    rollingLogObject.SaveLogMessage(logMessage);

                    // ASSERT
                    logVaultStub.LogObjects.Should().NotBeNull();
                    logVaultStub.LogObjects.Count.Should().Be(1);
                    logVaultStub.LogObjects[0].NameOrTitle.Should().Be(expectedObjectNameOrTitle);
                    logVaultStub.LogObjects[0].LogMessage.Should().Be(expectedObjectLogMessage);
                }


                [TestMethod]
                public void WhenTodaysLogObjectExistsAndLogMessageDoesNotFit_ExpectLogMessagePartAppendedToTodaysLogObjectAndRemainingInANewLogObject()
                {
                    var logObjectNamePrefix             = "Log-";
                    var logLine                         = "This line is a hundred characters in size 0123456789012345678901234567890123456789012345678901234\r\n";
                    var logMessage                      = logLine.RepeatWithLineCounter(100);
                    var logVaultStub                    = new LogVaultStub();

                    logVaultStub.LogObjects.Add(new LogObjectStub
                    {
                        NameOrTitle = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}",
                        LogMessage  = "Existing message\r\n",
                        ObjVer      = CreateObjVerWithID(1)
                    });

                    var expectedObject1NameOrTitle      = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}";
                    var expectedObject1LogMessage       = $"Existing message\r\n{logLine.RepeatWithLineCounter(95)}"; // 96 repeats are stored in the first LogObject
                    var expectedObject2NameOrTitle      = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd} (2)";
                    var expectedObject2LogMessage       = logLine.RepeatWithLineCounter(5, lineCounter: 96);

                    // ACT
                    var rollingLogObject = new RollingLogObject(logVaultStub, logObjectNamePrefix);
                    rollingLogObject.SaveLogMessage(logMessage);

                    // ASSERT
                    logVaultStub.LogObjects.Should().NotBeNull();
                    logVaultStub.LogObjects.Count.Should().Be(2);
                    logVaultStub.LogObjects[0].NameOrTitle.Should().Be(expectedObject1NameOrTitle);
                    logVaultStub.LogObjects[0].LogMessage.Should().Be(expectedObject1LogMessage);
                    logVaultStub.LogObjects[1].NameOrTitle.Should().Be(expectedObject2NameOrTitle);
                    logVaultStub.LogObjects[1].LogMessage.Should().Be(expectedObject2LogMessage);
                }


                [TestMethod]
                public void WhenTodaysLogObjectExistsButHasLessthanMinCharsForRollover_ExpectANewLogObjectCreated()
                {
                    var logObjectNamePrefix             = "Log-";
                    var logMessage                      = "This is a new log message that should end up with the new Log object";

                    var existinglogLine                 = "This line is a hundred characters in size 0123456789012345678901234567890123456789012345678901234\r\n";
                    var existingLogMessage              = existinglogLine.RepeatWithLineCounter(100).Substring(0, 9990);
                    var logVaultStub                    = new LogVaultStub();

                    logVaultStub.LogObjects.Add(new LogObjectStub
                    {
                        NameOrTitle = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}",
                        LogMessage  = existingLogMessage,
                        ObjVer      = CreateObjVerWithID(1)
                    });

                    var expectedObject1NameOrTitle      = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd}";
                    var expectedObject1LogMessage       = existingLogMessage;
                    var expectedObject2NameOrTitle      = $"{logObjectNamePrefix}{DateTime.Today:yyyy-MM-dd} (2)";
                    var expectedObject2LogMessage       = $"{logMessage}\r\n";

                    // ACT
                    var rollingLogObject = new RollingLogObject(logVaultStub, logObjectNamePrefix);
                    rollingLogObject.SaveLogMessage(logMessage);

                    // ASSERT
                    logVaultStub.LogObjects.Should().NotBeNull();
                    logVaultStub.LogObjects.Count.Should().Be(2);
                    logVaultStub.LogObjects[0].NameOrTitle.Should().Be(expectedObject1NameOrTitle);
                    logVaultStub.LogObjects[0].LogMessage.Should().Be(expectedObject1LogMessage);
                    logVaultStub.LogObjects[1].NameOrTitle.Should().Be(expectedObject2NameOrTitle);
                    logVaultStub.LogObjects[1].LogMessage.Should().Be(expectedObject2LogMessage);
                }
            }
        }
    }
}
