using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Dramatic.LogToMFiles.Common;

namespace Dramatic.LogToMFiles.Tests
{
    [TestClass]
    public class TakeSubstringUpTotheLastNewLineTests
    {
        [DataTestMethod]
        [DataRow("Een regel\r\nNog een regel\r\n", 0, 10, "Een regel\r")]
        [DataRow("Een regel\r\nNog een regel\r\n", 0, 15, "Een regel\r\n")]
        [DataRow("Een regel\r\nNog een regel\r\n", 0, 100, "Een regel\r\nNog een regel\r\n")]
        public void TakingSentencesFromSourceString_Should_ReturnExpectedResult(string sut, int index, int maxCharactersToTake, string expectedResult)
        {
            // ASSIGN

            // ACT
            var result = sut.TakeSubstringUpTotheLastNewLine(index, maxCharactersToTake);

            // ASSERT

            result.Should().NotBeNullOrEmpty();
            result.Should().Be(expectedResult);
        }




        // -------------------------------------------------------------------------------------------------------------------------
        // Test failures

        [TestMethod]
        public void WhenTaking0CharactersFromString_ExpectArgumentOutOfRangeException()
        //public void ThrowArgumentOutOfRangeException_When_TakingSentencesFromSourceStringWithMaxCharactersToTake0()
        {
            // ASSIGN
            string sut                      = "Een regel\r\nNog een regel\r\n";
            int index                       = 0;
            int maxCharactersToTake         = 0;
            string expectedExceptionMessage = "maxCharactersToTake must be 1 or more\r\nParameter name: maxCharactersToTake";

            // ACT
            Action act = () => sut.TakeSubstringUpTotheLastNewLine(index, maxCharactersToTake);

            // ASSERT
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage(expectedExceptionMessage);
        }


        [TestMethod]
        public void WhenIndexIsLargerThanSourceString_ExpectArgumentOutOfRangeException()
        {
            // ASSIGN
            string sut                      = "Een regel\r\nNog een regel\r\n";
            int index                       = 100;  // too large
            int maxCharactersToTake         = 0;
            string expectedExceptionMessage = "index must be less than sourceString.Length\r\nParameter name: index";

            // ACT
            Action act = () => sut.TakeSubstringUpTotheLastNewLine(index, maxCharactersToTake);

            // ASSERT
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage(expectedExceptionMessage);
        }
    }
}
