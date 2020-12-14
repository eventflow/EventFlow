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
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using Microsoft.AspNetCore.Http;

namespace EventFlow.AspNetCore.MetadataProviders
{
    public class AddUserClaimsMetadataProvider : IMetadataProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserClaimsMetadataOptions _options;

        public AddUserClaimsMetadataProvider(
            IHttpContextAccessor httpContextAccessor,
            IUserClaimsMetadataOptions options)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options;
        }

        public IEnumerable<KeyValuePair<string, string>> ProvideMetadata<TAggregate, TIdentity>(TIdentity id,
            IAggregateEvent aggregateEvent, IMetadata metadata) where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return Enumerable.Empty<KeyValuePair<string, string>>();

            return from claim in user.Claims
                where _options.IsIncluded(claim.Type)
                group claim by claim.Type
                into claimGroup
                let key = _options.GetKeyForClaimType(claimGroup.Key)
                let values = claimGroup.Select(c => c.Value)
                let joinedValues = string.Join(";", values)
                select new KeyValuePair<string, string>(key, joinedValues);
        }
    }
}
