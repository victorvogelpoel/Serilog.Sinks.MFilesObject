// TakeSubstringUpTotheLastNewLineTests.cs
// 3-9-2021
// Copyright 2021 Dramatic Development - Victor Vogelpoel
// If this works, it was written by Victor Vogelpoel (victor@victorvogelpoel.nl).
// If it doesn't, I don't know who wrote it.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            using (new AssertionScope())
            {
                result.Should().NotBeNullOrEmpty();
                result.Should().Be(expectedResult);
            }
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
