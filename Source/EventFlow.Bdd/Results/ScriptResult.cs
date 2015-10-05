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

using System;
using System.Collections.Generic;

namespace EventFlow.Bdd.Results
{
    public class ScriptResult
    {
        public string Name { get; }
        public StateResult GivenResult { get; }
        public StateResult WhenResult { get; }
        public StateResult ThenResult { get; }
        public bool Success { get; }

        public ScriptResult(
            string name,
            StateResult givenResult,
            StateResult whenResult,
            StateResult thenResult)
        {
            Name = name ?? string.Empty;
            GivenResult = givenResult;
            WhenResult = whenResult;
            ThenResult = thenResult;

            Success =
                (GivenResult == null || GivenResult.Success) &&
                (WhenResult == null || WhenResult.Success) &&
                (ThenResult == null || ThenResult.Success);
        }

        public IEnumerable<string> Print()
        {
            var count = 1;

            yield return $"SCENARIO {Name}";

            foreach (var state in new []
                {
                    new {Result = GivenResult, Title = "GIVEN"},
                    new {Result = WhenResult, Title = "WHEN"},
                    new {Result = ThenResult, Title = "THEN"},
                })
            {
                if (state.Result == null)
                {
                    continue;
                }

                yield return state.Title;
                foreach (var s in state.Result.Print())
                {
                    yield return $" {count,2} {s}";
                    count++;
                }
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Print());
        }
    }
}