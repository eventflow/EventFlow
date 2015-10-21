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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Queries;
using EventFlow.Queries;
using EventFlow.ReadStores.InMemory;

namespace EventFlow.Examples.Shipping.Queries.InMemory.Cargos.QueryHandlers
{
    public class GetCargosQueryHandler : IQueryHandler<GetCargosQuery, IReadOnlyCollection<Cargo>>
    {
        private readonly IInMemoryReadStore<CargoReadModel> _readStore;

        public GetCargosQueryHandler(
            IInMemoryReadStore<CargoReadModel> readStore)
        {
            _readStore = readStore;
        }

        public async Task<IReadOnlyCollection<Cargo>> ExecuteQueryAsync(GetCargosQuery query, CancellationToken cancellationToken)
        {
            var cargoIds = new HashSet<CargoId>(query.CargoIds);
            var cargoReadModels = await _readStore.FindAsync(
                rm => cargoIds.Contains(rm.Id),
                cancellationToken)
                .ConfigureAwait(false);
            return cargoReadModels.Select(rm => rm.ToCargo()).ToList();
        }
    }
}