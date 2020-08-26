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
using EventFlow.Core;
using EventFlow.PostgreSql.Connections;
using Npgsql;

namespace EventFlow.PostgreSql.RetryStrategies
{
    public class PostgreSqlErrorRetryStrategy : IPostgreSqlErrorRetryStrategy
    {
        private readonly IPostgreSqlConfiguration _configuration;

        public PostgreSqlErrorRetryStrategy(
            IPostgreSqlConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Retry ShouldThisBeRetried(Exception exception, TimeSpan totalExecutionTime, int currentRetryCount)
        {
            var postGreSqlException = exception as PostgresException;
            if (postGreSqlException == null || currentRetryCount > _configuration.TransientRetryCount)
            {
                return Retry.No;
            }

            switch (postGreSqlException.SqlState)
            {
                //https://www.postgresql.org/docs/9.4/static/errcodes-appendix.html
                //deadlock_detected

                case "40P01":


                //https://www.postgresql.org/docs/9.4/static/errcodes-appendix.html
                //active_sql_transaction

                case "25001": 
                    return Retry.YesAfter(_configuration.TransientRetryDelay.PickDelay());
                    
                default:
                    return Retry.No;
            }
        }
    }
}
