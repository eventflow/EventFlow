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

using System.Collections.Generic;
using System.Linq;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Entities;
using EventFlow.Examples.Shipping.Extensions;
using EventFlow.Specifications;

namespace EventFlow.Examples.Shipping.Domain.Model.CargoModel.Specifications
{
    public class TransportLegsAreConnectedSpecification : Specification<IReadOnlyCollection<TransportLeg>>
    {
        protected override IEnumerable<string> IsNotSatisfiedBecause(IReadOnlyCollection<TransportLeg> obj)
        {
            return obj
                .Zip(obj.Skip(1), AreConnectedEvaluator)
                .SelectMany(s => s.ToList());
        }

        private static IEnumerable<string> AreConnectedEvaluator(TransportLeg previous, TransportLeg next)
        {
            if (previous.UnloadLocation != next.LoadLocation)
            {
                yield return Error(previous, next, $"Unload '{previous.UnloadLocation}' != load {next.LoadLocation}");
            }

            if (previous.UnloadTime.IsAfter(next.LoadTime))
            {
                yield return Error(previous, next, $"Unload '{previous.UnloadTime}' is after load {next.LoadTime}");
            }
        }

        private static string Error(TransportLeg previous, TransportLeg next, string validationError)
        {
            return $"{previous.Id} -> {next.Id}: {validationError}";
        }
    }
}