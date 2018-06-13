using System;
using EventFlow.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public class EntityFrameworkConfiguration : IEntityFrameworkConfiguration
    {
        private Action<IServiceRegistration> _registerUniqueConstraintViolationDetector;

        private EntityFrameworkConfiguration()
        {
            UseUniqueConstraintViolationDetection(exception =>
                exception.InnerException?.Message?.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public EntityFrameworkConfiguration UseUniqueConstraintViolationDetection<T>()
            where T : class, IUniqueConstraintViolationDetector
        {
            _registerUniqueConstraintViolationDetector = s => s.Register<IUniqueConstraintViolationDetector, T>();
            return this;
        }

        public EntityFrameworkConfiguration UseUniqueConstraintViolationDetection(Func<Exception, bool> exceptionIsUniqueConstraintViolation)
        {
            var detector = new DelegateUniqueConstraintViolationDetector(exceptionIsUniqueConstraintViolation);
            _registerUniqueConstraintViolationDetector = s => s.Register<IUniqueConstraintViolationDetector>(_ => detector);
            return this;
        }

        public static EntityFrameworkConfiguration New => new EntityFrameworkConfiguration();

        void IEntityFrameworkConfiguration.Apply(IServiceRegistration serviceRegistration)
        {
            _registerUniqueConstraintViolationDetector(serviceRegistration);
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
