// The MIT License (MIT)
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Examples.Library.Aggregates.Books.ReadModels;
using EventFlow.Examples.Library.ValueObjects;
using EventFlow.Queries;
using EventFlow.ReadStores.InMemory;

namespace EventFlow.Examples.Library.Aggregates.Books.Queries
{
    public class FindBookByTitle : IQuery<Book>
    {
        public Title Title { get; }

        public FindBookByTitle(
            Title title)
        {
            Title = title;
        }
    }

    public class FindBookByTitleHandlerForInMemory : IQueryHandler<FindBookByTitle, Book>
    {
        private readonly IInMemoryReadStore<BookReadModel> _readStore;

        public FindBookByTitleHandlerForInMemory(
            IInMemoryReadStore<BookReadModel> readStore)
        {
            _readStore = readStore;
        }

        public async Task<Book> ExecuteQueryAsync(FindBookByTitle query, CancellationToken cancellationToken)
        {
            var readModels = await _readStore.FindAsync(b => b.Title.Contains(query.Title.Value), cancellationToken).ConfigureAwait(false);
            var readModel = readModels.SingleOrDefault();
            return readModel == null
                ? null
                : new Book(
                    new Isbn(readModel.Isbn),
                    new Title(readModel.Title));
        }
    }
}
