using System;
using EventFlow.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public class EntityFrameworkConfiguration : IEntityFrameworkConfiguration
    {
        private int _bulkDeletionBatchSize = 1000;
        private Action<IServiceRegistration> _registerUniqueConstraintDetectionStrategy;

        public string ConnectionString { get; private set; }

        private EntityFrameworkConfiguration()
        {
            UseUniqueConstraintDetectionStrategy(exception =>
                exception.InnerException?.Message?.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static EntityFrameworkConfiguration New => new EntityFrameworkConfiguration();

        int IEntityFrameworkConfiguration.BulkDeletionBatchSize => _bulkDeletionBatchSize;

        void IEntityFrameworkConfiguration.Apply(IServiceRegistration serviceRegistration)
        {
            serviceRegistration.Register<IEntityFrameworkConfiguration>(s => this);
            _registerUniqueConstraintDetectionStrategy(serviceRegistration);
        }

        public EntityFrameworkConfiguration SetBulkDeletionBatchSize(int batchSize)
        {
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            _bulkDeletionBatchSize = batchSize;
            return this;
        }

        public EntityFrameworkConfiguration UseUniqueConstraintDetectionStrategy<T>()
            where T : class, IUniqueConstraintDetectionStrategy
        {
            _registerUniqueConstraintDetectionStrategy = s => s.Register<IUniqueConstraintDetectionStrategy, T>();
            return this;
        }

        public EntityFrameworkConfiguration UseUniqueConstraintDetectionStrategy(
            Func<Exception, bool> exceptionIsUniqueConstraintViolation)
        {
            var strategy = new DelegateUniqueConstraintDetectionStrategy(exceptionIsUniqueConstraintViolation);
            _registerUniqueConstraintDetectionStrategy =
                s => s.Register<IUniqueConstraintDetectionStrategy>(_ => strategy);
            return this;
        }

        private class DelegateUniqueConstraintDetectionStrategy : IUniqueConstraintDetectionStrategy
        {
            private readonly Func<DbUpdateException, bool> _callback;

            public DelegateUniqueConstraintDetectionStrategy(Func<DbUpdateException, bool> callback)
            {
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public bool IsUniqueConstraintViolation(DbUpdateException exception)
            {
                return _callback(exception);
            }
        }

        public EntityFrameworkConfiguration SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }
    }
}
