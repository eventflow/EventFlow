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

using EventFlow.Configuration.Registrations;
using EventFlow.Queries;
using EventFlow.ReadStores.MsSql.Queries;

namespace EventFlow.ReadStores.MsSql.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static EventFlowOptions UseMssqlReadModel<TReadModel, TReadModelLocator>(this EventFlowOptions eventFlowOptions)
            where TReadModel : IMssqlReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            eventFlowOptions.RegisterServices(f =>
                {
                    if (!f.HasRegistrationFor<IReadModelSqlGenerator>())
                    {
                        f.Register<IReadModelSqlGenerator, ReadModelSqlGenerator>(Lifetime.Singleton);
                    }
                    f.Register<IReadModelStore, MssqlReadModelStore<TReadModel, TReadModelLocator>>();
                    f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>, MsSqlReadModelByIdQueryHandler<TReadModel>>();
                });

            return eventFlowOptions;
        }
    }
}
