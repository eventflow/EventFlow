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
using System.Linq;
using EventFlow.Extensions;

namespace EventFlow.Bdd.Results.Formatters
{
    public class TextResultFormatter : ITextResultFormatter
    {
        public string Format(ScriptResult scriptResult)
        {
            return string.Join(Environment.NewLine, GenerateLines(scriptResult));
        }

        private static IEnumerable<string> GenerateLines(ScriptResult scriptResult)
        {
            var count = 1;

            yield return $"SCENARIO {scriptResult.Name}";

            foreach (var state in new[]
                {
                    new {Result = scriptResult.GivenResult, Title = "GIVEN"},
                    new {Result = scriptResult.WhenResult, Title = "WHEN"},
                    new {Result = scriptResult.ThenResult, Title = "THEN"},
                })
            {
                if (state.Result == null)
                {
                    continue;
                }

                yield return state.Title;
                foreach (var s in GenerateLines(state.Result))
                {
                    yield return $" {count,2} {s}";
                    count++;
                }
            }
        }

        private static IEnumerable<string> GenerateLines(StateResult stateResult)
        {
            return stateResult.StepResults.Select(GenerateLine);
        }

        private static string GenerateLine(StepResult stepResult)
        {
            var messageParts = new List<string>
                {
                    stepResult.Name,
                    $"{stepResult.ExecutionResult.ToString().ToUpperInvariant()}"
                };

            if (stepResult.Exception != null)
            {
                messageParts.Add($"{stepResult.Exception.GetType().PrettyPrint()}: {stepResult.Exception.Message}");
            }

            return string.Join(" ", messageParts);
        }
    }
}