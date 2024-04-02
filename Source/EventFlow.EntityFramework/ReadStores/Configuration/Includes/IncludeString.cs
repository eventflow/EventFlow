// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.ReadStores.Configuration.Includes
{
    public sealed class IncludeString<TReadModel>
        : IApplyQueryableConfiguration<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        private readonly IApplyQueryableConfiguration<TReadModel> _source;
        private readonly string _navigationPropertyPath;

        internal IncludeString(
            IApplyQueryableConfiguration<TReadModel> source,
            string navigationPropertyPath)
        {
            _source = source;
            _navigationPropertyPath = navigationPropertyPath;
        }

        IQueryable<TReadModel> IApplyQueryableConfiguration<TReadModel>.Apply(
            IQueryable<TReadModel> queryable) =>
            _source.Apply(queryable).Include(_navigationPropertyPath);
    }
}