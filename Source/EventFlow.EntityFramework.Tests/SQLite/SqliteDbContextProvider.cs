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
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    public class SqliteDbContextProvider : IDbContextProvider<TestDbContext>, IDisposable
    {
        private readonly DbContextOptions<TestDbContext> _options;
        private readonly SqliteConnection _connection;

        public SqliteDbContextProvider()
        {
            // In-memory database only exists while the connection is open
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;
        }

        public TestDbContext CreateContext()
        {
            var context = new TestDbContext(_options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}