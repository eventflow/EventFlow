﻿// The MIT License (MIT)
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

using System.ComponentModel.DataAnnotations.Schema;
using EventFlow.MsSql.Tests.ReadModels;
using EventFlow.ReadStores.MsSql;
using EventFlow.Test;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.UnitTests.ReadModels
{
    public class ReadModelSqlGeneratorTests : TestsFor<ReadModelSqlGenerator>
    {
        [Table("TestName")]
        public class TableTestReadModel : MssqlReadModel { }

        [Test]
        public void CreateInsertSql_ProducesCorrectSql_WithTableAttribute()
        {
            // Act
            var sql = Sut.CreateInsertSql<TableTestReadModel>();

            // Assert
            sql.Should().Be(
                "INSERT INTO [TestName] " +
                "(AggregateId, CreateTime, LastAggregateSequenceNumber, LastGlobalSequenceNumber, UpdatedTime) " +
                "VALUES " +
                "(@AggregateId, @CreateTime, @LastAggregateSequenceNumber, @LastGlobalSequenceNumber, @UpdatedTime)");
        }

        [Test]
        public void CreateInsertSql_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateInsertSql<TestAggregateReadModel>();

            // Assert
            sql.Should().Be(
                "INSERT INTO [ReadModel-TestAggregate] " +
                "(AggregateId, CreateTime, DomainErrorAfterFirstReceived, LastAggregateSequenceNumber, LastGlobalSequenceNumber, PingsReceived, UpdatedTime) " +
                "VALUES " +
                "(@AggregateId, @CreateTime, @DomainErrorAfterFirstReceived, @LastAggregateSequenceNumber, @LastGlobalSequenceNumber, @PingsReceived, @UpdatedTime)");
        }

        [Test]
        public void CreateUpdateSql_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateUpdateSql<TestAggregateReadModel>();

            // Assert
            sql.Should().Be(
                "UPDATE [ReadModel-TestAggregate] SET " +
                "CreateTime = @CreateTime, DomainErrorAfterFirstReceived = @DomainErrorAfterFirstReceived, " +
                "LastAggregateSequenceNumber = @LastAggregateSequenceNumber, LastGlobalSequenceNumber = @LastGlobalSequenceNumber, " +
                "PingsReceived = @PingsReceived, UpdatedTime = @UpdatedTime " +
                "WHERE AggregateId = @AggregateId");
        }

        [Test]
        public void CreateSelectSql_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateSelectSql<TestAggregateReadModel>();

            // Assert
            sql.Should().Be("SELECT * FROM [ReadModel-TestAggregate] WHERE AggregateId = @AggregateId");
        }
    }
}
