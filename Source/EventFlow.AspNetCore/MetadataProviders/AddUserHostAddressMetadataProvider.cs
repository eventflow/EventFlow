// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace EventFlow.Aspnetcore.MetadataProviders
{
    public class AddUserHostAddressMetadataProvider : IMetadataProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly IEnumerable<string> HeaderPriority = new[]
            {
                "X-Forwarded-For",
                "HTTP_X_FORWARDED_FOR",
                "X-Real-IP",
                "REMOTE_ADDR"
            };

        public AddUserHostAddressMetadataProvider(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<KeyValuePair<string, string>> ProvideMetadata<TAggregate, TIdentity>(
            TIdentity id,
            IAggregateEvent aggregateEvent,
            IMetadata metadata)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            yield return new KeyValuePair<string, string>("remote_ip_address", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());

            var headerInfo = HeaderPriority
                .Select(h =>
                    {
                        StringValues value;
                        var address = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(h, out value)
                            ? string.Join(string.Empty, value)
                            : string.Empty;
                        return new {Header = h, Address = address};
                    })
                .FirstOrDefault(a => !string.IsNullOrEmpty(a.Address));

            if (headerInfo == null)
            {
                yield break;
            }

            yield return new KeyValuePair<string, string>("user_host_address", headerInfo.Address);
            yield return new KeyValuePair<string, string>("user_host_address_source_header", headerInfo.Header);
        }
    }
}
