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

using System;
using System.ComponentModel.DataAnnotations.Schema;
using EventFlow.ReadStores;
using EventFlow.Sql.ReadModels;
using EventFlow.Sql.ReadModels.Attributes;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Sql.Tests.UnitTests.ReadModels
{
    [Category(Categories.Unit)]
    public class ReadModelSqlGeneratorTests : TestsFor<ReadModelSqlGenerator>
    {
        [Test]
        public void CreateInsertSql_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateInsertSql<TestAttributesReadModel>();

            // Assert
            sql.Should().Be("INSERT INTO [ReadModel-TestAttributes] ([Id], [UpdatedTime]) VALUES (@Id, @UpdatedTime)");
        }

        [Test]
        public void CreateUpdateSql_WithoutVersion_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateUpdateSql<TestAttributesReadModel>().Trim();

            // Assert
            sql.Should().Be("UPDATE [ReadModel-TestAttributes] SET [UpdatedTime] = @UpdatedTime WHERE [Id] = @Id");
        }

        [Test]
        public void CreateUpdateSql_WithVersion_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateUpdateSql<TestVersionedAttributesReadModel>().Trim();

            // Assert
            sql.Should().Be("UPDATE [ReadModel-TestVersionedAttributes] SET [FancyVersion] = @FancyVersion WHERE [CoolId] = @CoolId AND [FancyVersion] = @_PREVIOUS_VERSION");
        }

        [Test]
        public void CreateSelectSql_ProducesCorrectSql()
        {
            // Act
            var sql = Sut.CreateSelectSql<TestAttributesReadModel>();

            // Assert
            sql.Should().Be("SELECT * FROM [ReadModel-TestAttributes] WHERE Id = @EventFlowReadModelId");
        }

        [Test]
        public void GetTableName_UsesTableAttribute()
        {
            // Act
            var tableName = Sut.GetTableName<TestTableAttributeReadModel>();

            // Assert
            tableName.Should().Be("[doh].[Fancy]");
        }

        public class TestAttributesReadModel : IReadModel
        {
            [SqlReadModelIdentityColumn]
            public string Id { get; set; }

            public DateTimeOffset UpdatedTime { get; set; }

            [SqlReadModelIgnoreColumn]
            public string Secret { get; set; }
        }

        public class TestVersionedAttributesReadModel : IReadModel
        {
            [SqlReadModelIdentityColumn]
            public string CoolId { get; set; }

            [SqlReadModelVersionColumn]
            public string FancyVersion { get; set; }
        }

        [Table("Fancy", Schema = "doh")]
        public class TestTableAttributeReadModel : IReadModel
        {
        }
    }
}