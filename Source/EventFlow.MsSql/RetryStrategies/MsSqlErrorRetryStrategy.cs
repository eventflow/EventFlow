﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
// 

using System;
using System.Data.SqlClient;
using EventFlow.Core;
using EventFlow.Logging;

namespace EventFlow.MsSql.RetryStrategies
{
    public class MsSqlErrorRetryStrategy : IMsSqlErrorRetryStrategy
    {
        private readonly ILog _log;
        private readonly IMsSqlConfiguration _msSqlConfiguration;
        private static readonly Random Random = new Random();

        public MsSqlErrorRetryStrategy(
            ILog log,
            IMsSqlConfiguration msSqlConfiguration)
        {
            _log = log;
            _msSqlConfiguration = msSqlConfiguration;
        }

        public virtual Retry ShouldThisBeRetried(Exception exception, TimeSpan totalExecutionTime, int currentRetryCount)
        {
            var sqlException = exception as SqlException;
            if (sqlException == null || currentRetryCount > 2)
            {
                return Retry.No;
            }

            switch (sqlException.Number)
            {
                // SQL Error Code: 40501
                // The service is currently busy. Retry the request after 10 seconds.
                case 40501:
                    {
                        var delay = TimeSpan.FromMilliseconds(5000 + 10000 * Random.NextDouble());
                        _log.WarnFormat(
                            "MSSQL server returned error 40501 which means it too busy! Trying to wait {0:0.###} (random between 5 and 15 seconds)",
                            delay.TotalSeconds);
                        return Retry.YesAfter(delay);
                    }

                // SQL Error Code: 40197
                // The service has encountered an error processing your request. Please try again.
                case 40197:
                
                // SQL Error Code: 10053
                // A transport-level error has occurred when receiving results from the server.
                // An established connection was aborted by the software in your host machine.
                case 10053:

                // SQL Error Code: 10054
                // A transport-level error has occurred when sending the request to the server.
                // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                case 10054:

                // SQL Error Code: 10060
                // A network-related or instance-specific error occurred while establishing a connection to SQL Server.
                // The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server
                // is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed
                // because the connected party did not properly respond after a period of time, or established connection failed
                // because connected host has failed to respond.)"}
                case 10060:

                // SQL Error Code: 40613
                // Database XXXX on server YYYY is not currently available. Please retry the connection later. If the problem persists, contact customer
                // support, and provide them the session tracing ID of ZZZZZ.
                case 40613:
                
                // SQL Error Code: 40143
                // The service has encountered an error processing your request. Please try again.
                case 40143:
                
                // SQL Error Code: 233
                // The client was unable to establish a connection because of an error during connection initialization process before login.
                // Possible causes include the following: the client tried to connect to an unsupported version of SQL Server; the server was too busy
                // to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections) on the server.
                // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                case 233:
                
                // SQL Error Code: 64
                // A connection was successfully established with the server, but then an error occurred during the login process.
                // (provider: TCP Provider, error: 0 - The specified network name is no longer available.)
                case 64:
                    return Retry.YesAfter(_msSqlConfiguration.TransientRetryDelay.PickDelay());

                default:
                    return Retry.No;
            }
        }
    }
}