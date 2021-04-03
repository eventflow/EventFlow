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
using EventFlow.Core;
using EventFlow.Queries;
using EventFlow.TestHelpers;
using EventFlow.Tests.UnitTests.Core.VersionedTypes;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Queries
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class QueryDefinitionServiceTests : VersionedTypeDefinitionServiceTestSuite<QueryDefinitionService, IQuery, QueryVersionAttribute, QueryDefinition>
    {
        [QueryVersion("Fancy", 42)]
        public class TestQueryWithLongName : IQuery<IIdentity>
        {
        }

        public class TestQuery : IQuery<IIdentity>
        {
        }

        public class TestQueryV2 : IQuery<IIdentity>
        {
        }

        public class OldTestQueryV5 : IQuery<IIdentity>
        {
        }

        public override IEnumerable<VersionTypeTestCase> GetTestCases()
        {
            yield return new VersionTypeTestCase
            {
                Name = "TestQuery",
                Type = typeof(TestQuery),
                Version = 1,
            };
            yield return new VersionTypeTestCase
            {
                Name = "TestQuery",
                Type = typeof(TestQueryV2),
                Version = 2,
            };
            yield return new VersionTypeTestCase
            {
                Name = "TestQuery",
                Type = typeof(OldTestQueryV5),
                Version = 5,
            };
            yield return new VersionTypeTestCase
            {
                Name = "Fancy",
                Type = typeof(TestQueryWithLongName),
                Version = 42,
            };
        }
    }
}
