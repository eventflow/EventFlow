// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Text;

namespace EventFlow.Bdd.Results
{
    public class ScriptResult
    {
        public StateResult GivenResult { get; }
        public StateResult WhenResult { get; }
        public StateResult ThenResult { get; }
        public bool Success { get; }

        public ScriptResult(
            StateResult givenResult,
            StateResult whenResult,
            StateResult thenResult)
        {
            GivenResult = givenResult;
            WhenResult = whenResult;
            ThenResult = thenResult;

            Success =
                GivenResult != null &&
                GivenResult.Success &&
                WhenResult != null &&
                WhenResult.Success &&
                ThenResult != null &&
                ThenResult.Success;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            if (GivenResult != null)
            {
                stringBuilder.AppendLine("GIVEN");
                stringBuilder.AppendLine(GivenResult.ToString());
            }
            if (WhenResult != null)
            {
                stringBuilder.AppendLine("WHEN");
                stringBuilder.AppendLine(WhenResult.ToString());
            }
            if (ThenResult != null)
            {
                stringBuilder.AppendLine("THEN");
                stringBuilder.AppendLine(ThenResult.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}