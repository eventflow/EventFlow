﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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

using Npgsql;
using System;
using System.Data;
using System.Data.SqlClient;

namespace EventFlow.TestHelpers.PostgreSql
{
    public class PostgressSqlDatabase : IPostgreSqlDatabase
    {
        public PostgressSqlDatabase(PostgreSqlConnectionString connectionString, bool dropOnDispose)
        {
            ConnectionString = connectionString;
            DropOnDispose = dropOnDispose;
            ConnectionString.Ping();
        }

        public PostgreSqlConnectionString ConnectionString { get; }
        public bool DropOnDispose { get; }

        public void Dispose()
        {
            if (DropOnDispose)
            {
                var masterConnectionString = ConnectionString.NewConnectionString("postgres");
                masterConnectionString.Execute($"DROP DATABASE \"{ConnectionString.Database}\";");
            }
        }

        public void Ping()
        {
            ConnectionString.Ping();
        }

        public T WithConnection<T>(Func<NpgsqlConnection, T> action)
        {
            return ConnectionString.WithConnection(action);
        }

        public void Execute(string sql)
        {
            ConnectionString.Execute(sql);
        }

        public void WithConnection(Action<NpgsqlConnection> action)
        {
            ConnectionString.WithConnection(action);
        }
    }
}
