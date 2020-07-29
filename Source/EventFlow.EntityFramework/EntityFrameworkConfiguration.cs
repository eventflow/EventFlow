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
using EventFlow.Configuration;

namespace EventFlow.EntityFramework
{
    public class EntityFrameworkConfiguration : IEntityFrameworkConfiguration
    {
        private Action<IServiceRegistration> _registerUniqueConstraintDetectionStrategy;
        private Action<IServiceRegistration> _registerBulkOperationConfiguration;

        public static EntityFrameworkConfiguration New => new EntityFrameworkConfiguration();

        private EntityFrameworkConfiguration()
        {
            UseUniqueConstraintDetectionStrategy<DefaultUniqueConstraintDetectionStrategy>();
            UseBulkOperationConfiguration<DefaultBulkOperationConfiguration>();
        }

        void IEntityFrameworkConfiguration.Apply(IServiceRegistration serviceRegistration)
        {
            serviceRegistration.Register<IEntityFrameworkConfiguration>(s => this);
            _registerUniqueConstraintDetectionStrategy(serviceRegistration);
            _registerBulkOperationConfiguration(serviceRegistration);
        }

        public EntityFrameworkConfiguration UseBulkOperationConfiguration<T>()
            where T : class, IBulkOperationConfiguration
        {
            _registerBulkOperationConfiguration = s => s.Register<IBulkOperationConfiguration, T>();
            return this;
        }

        public EntityFrameworkConfiguration UseUniqueConstraintDetectionStrategy<T>()
            where T : class, IUniqueConstraintDetectionStrategy
        {
            _registerUniqueConstraintDetectionStrategy = s => s.Register<IUniqueConstraintDetectionStrategy, T>();
            return this;
        }
    }
}