// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Jobs;
using EventFlow.TestHelpers;
using EventFlow.Tests.UnitTests.Core.VersionedTypes;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Jobs
{
    [Category(Categories.Unit)]
    public class JobDefinitionServiceTests : VersionedTypeDefinitionServiceTestSuite<JobDefinitionService, IJob, JobVersionAttribute, JobDefinition>
    {
        [JobVersion("Fancy", 42)]
        public class TestJobWithLongName : IJob
        {
            public Task ExecuteAsync(IResolver resolver, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }

        public class TestJob : IJob
        {
            public Task ExecuteAsync(IResolver resolver, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }

        public class TestJobV2 : IJob
        {
            public Task ExecuteAsync(IResolver resolver, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }

        public class OldTestJobV5 : IJob
        {
            public Task ExecuteAsync(IResolver resolver, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }

        public override IEnumerable<VersionTypeTestCase> GetTestCases()
        {
            yield return new VersionTypeTestCase
                {
                    Name = "TestJob",
                    Type = typeof(TestJob),
                    Version = 1,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "TestJob",
                    Type = typeof(TestJobV2),
                    Version = 2,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "TestJob",
                    Type = typeof(OldTestJobV5),
                    Version = 5,
                };
            yield return new VersionTypeTestCase
                {
                    Name = "Fancy",
                    Type = typeof(TestJobWithLongName),
                    Version = 42,
                };
        }
    }
}