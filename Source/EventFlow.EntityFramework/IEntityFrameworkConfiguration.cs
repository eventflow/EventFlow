﻿using EventFlow.Configuration;

namespace EventFlow.EntityFramework
{
    public interface IEntityFrameworkConfiguration
    {
        string ConnectionString { get; }
        int BulkDeletionBatchSize { get; }
        void Apply(IServiceRegistration serviceRegistration);
    }
}
