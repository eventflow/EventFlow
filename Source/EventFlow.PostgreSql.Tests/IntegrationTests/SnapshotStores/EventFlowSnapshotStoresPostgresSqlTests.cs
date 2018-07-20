﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rida Messaoudene
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

using System.Linq;

using EventFlow.PostgreSql.SnapshotStores;
using EventFlow.TestHelpers;

using FluentAssertions;

using NUnit.Framework;

namespace EventFlow.PostgreSql.Tests.IntegrationTests.SnapshotStores
{
    [Category(Categories.Integration)]
    public class EventFlowSnapshotStoresPostgreSqlTests
    {
        [Test]
        public void GetSqlScripts()
        {
            // Act
            var sqlScripts = EventFlowSnapshotStoresPostgreSql.GetSqlScripts().ToDictionary(s => s.Name, s => s);

            // Assert
            sqlScripts.Should().HaveCount(1);
            sqlScripts.Should().ContainKey("SnapshotStores.Scripts.0001 - Create EventFlowSnapshots.sql");
        }
    }
}