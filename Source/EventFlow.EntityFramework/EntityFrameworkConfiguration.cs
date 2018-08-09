using System;
using EventFlow.Configuration;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public class EntityFrameworkConfiguration : IEntityFrameworkConfiguration
    {
        private Action<IServiceRegistration> _registerUniqueConstraintDetectionStrategy;

        public static EntityFrameworkConfiguration New => new EntityFrameworkConfiguration();

        public string ConnectionString { get; private set; }
        
        private EntityFrameworkConfiguration()
        {
            LinqToDBForEFTools.Initialize();
            UseUniqueConstraintDetectionStrategy<DefaultUniqueConstraintDetectionStrategy>();
        }

        void IEntityFrameworkConfiguration.Apply(IServiceRegistration serviceRegistration)
        {
            serviceRegistration.Register<IEntityFrameworkConfiguration>(s => this);
            _registerUniqueConstraintDetectionStrategy(serviceRegistration);
        }

        public EntityFrameworkConfiguration UseUniqueConstraintDetectionStrategy<T>()
            where T : class, IUniqueConstraintDetectionStrategy
        {
            _registerUniqueConstraintDetectionStrategy = s => s.Register<IUniqueConstraintDetectionStrategy, T>();
            return this;
        }

        public EntityFrameworkConfiguration SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }
    }
}
