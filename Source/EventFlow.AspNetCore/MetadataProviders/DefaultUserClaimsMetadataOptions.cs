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
using System.Reflection;
using System.Security.Claims;

namespace EventFlow.AspNetCore.MetadataProviders
{
    public class DefaultUserClaimsMetadataOptions : IUserClaimsMetadataOptions
    {
        private static readonly Dictionary<string, string> ClaimTypeMapping
            = (from field in typeof(ClaimTypes)
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                let key = (string) field.GetValue(null)
                let value = field.Name.ToLowerInvariant()
                select new KeyValuePair<string, string>(key, value))
            .ToDictionary(k => k.Key, k => k.Value);

        public DefaultUserClaimsMetadataOptions(IEnumerable<string> includedClaimTypes = null)
        {
            if (includedClaimTypes == null) return;

            var hashSet = new HashSet<string>(includedClaimTypes);
            if (hashSet.Any())
            {
                IncludedClaimTypes = hashSet;
            }
        }

        private HashSet<string> IncludedClaimTypes { get; } = new HashSet<string>
        {
            ClaimTypes.Sid,
            ClaimTypes.Role
        };

        public bool IsIncluded(string claimType)
        {
            return IncludedClaimTypes.Contains(claimType);
        }

        public string GetKeyForClaimType(string claimType)
        {
            var type = ClaimTypeMapping.TryGetValue(claimType, out var mappedType)
                ? mappedType
                : claimType;

            return $"user_claim[{type}]";
        }
    }
}