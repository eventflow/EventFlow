using System;
using EventFlow.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public class EntityFrameworkConfiguration : IEntityFrameworkConfiguration
    {
        private int _readModelDeletionBatchSize = 1000;
        private Action<IServiceRegistration> _registerUniqueConstraintViolationDetector;

        private EntityFrameworkConfiguration()
        {
            UseUniqueConstraintViolationDetection(exception =>
                exception.InnerException?.Message?.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static EntityFrameworkConfiguration New => new EntityFrameworkConfiguration();

        int IEntityFrameworkConfiguration.ReadModelDeletionBatchSize => _readModelDeletionBatchSize;

        void IEntityFrameworkConfiguration.Apply(IServiceRegistration serviceRegistration)
        {
            serviceRegistration.Register<IEntityFrameworkConfiguration>(s => this);
            _registerUniqueConstraintViolationDetector(serviceRegistration);
        }

        public EntityFrameworkConfiguration SetReadModelDeletionBatchSize(int batchSize)
        {
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            _readModelDeletionBatchSize = batchSize;
            return this;
        }

        public EntityFrameworkConfiguration UseUniqueConstraintViolationDetection<T>()
            where T : class, IUniqueConstraintViolationDetector
        {
            _registerUniqueConstraintViolationDetector = s => s.Register<IUniqueConstraintViolationDetector, T>();
            return this;
        }

        public EntityFrameworkConfiguration UseUniqueConstraintViolationDetection(
            Func<Exception, bool> exceptionIsUniqueConstraintViolation)
        {
            var detector = new DelegateUniqueConstraintViolationDetector(exceptionIsUniqueConstraintViolation);
            _registerUniqueConstraintViolationDetector =
                s => s.Register<IUniqueConstraintViolationDetector>(_ => detector);
            return this;
        }

        private class DelegateUniqueConstraintViolationDetector : IUniqueConstraintViolationDetector
        {
            private readonly Func<DbUpdateException, bool> _callback;

            public DelegateUniqueConstraintViolationDetector(Func<DbUpdateException, bool> callback)
            {
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public bool IsUniqueContraintException(DbUpdateException exception)
            {
                return _callback(exception);
            }
        }
    }
}
