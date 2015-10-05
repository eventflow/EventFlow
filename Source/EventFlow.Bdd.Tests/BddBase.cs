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

using EventFlow.Bdd.Extensions;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.Bdd.Tests
{
    public abstract class BddBase : Test
    {
        protected IRootResolver Resolver { get; private set; }
        private IScenario _scenario;

        [SetUp]
        public void SetUpBdd()
        {
            Resolver = EventFlowOptions.New
                .AddDefaults(EventFlowTestHelpers.Assembly)
                .TestWithBdd()
                .CreateResolver();
        }

        [TearDown]
        public void TearDownBdd()
        {
            Resolver.DisposeSafe();
            _scenario.DisposeSafe();
        }

        protected IScenario Scenario(string named)
        {
            _scenario = Resolver.Scenario(named);
            return _scenario;
        }
    }
}