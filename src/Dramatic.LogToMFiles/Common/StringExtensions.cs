// StringExtensions.cs
// 24-11-2021
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

namespace Dramatic.LogToMFiles
{
    /// <summary>
    /// A String extension to take a number of lines that fit the characters limit.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Take at most maxCharactersToTake from the source string from index and return up to the last whole sentence.
        /// If no newline is found, maxCharactersToTake characters are returned from the index location in the sourceString.
        /// </summary>
        /// <param name="sourceString">The string to take sentences from</param>
        /// <param name="index">the location to take the sentences from</param>
        /// <param name="maxCharactersToTake">The maximum number of characters to take.</param>
        /// <returns>Part of the sourcestring from the index up to either the last sentence or maxCharactersToTake characters</returns>
        public static string TakeSubstringUpTotheLastNewLine(this string sourceString, int index, int maxCharactersToTake)
        {
            if (index >= sourceString.Length)   { throw new ArgumentOutOfRangeException(nameof(index), "index must be less than sourceString.Length"); }
            if (maxCharactersToTake <= 0)       { throw new ArgumentOutOfRangeException(nameof(maxCharactersToTake), "maxCharactersToTake must be 1 or more"); }

            int newLineCharSize         = Environment.NewLine.Length;
            var substring               = sourceString.Substring(index, Math.Min(sourceString.Length-index, maxCharactersToTake));

            var lastNLOccurrence        = substring.LastIndexOf(Environment.NewLine, StringComparison.Ordinal);
            if (lastNLOccurrence > 0 && lastNLOccurrence+newLineCharSize < substring.Length)
            {
                // NL found and clip the substring, including the NewLine.
                substring = substring.Substring(0, lastNLOccurrence+newLineCharSize);
            }
            // else: NewLine was not found in this substring. Simply return the entire substring...

            return substring;
        }
    }
}
