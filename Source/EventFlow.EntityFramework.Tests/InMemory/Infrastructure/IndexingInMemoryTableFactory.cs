using System.Linq;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Storage.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EventFlow.EntityFramework.Tests.InMemory.Infrastructure
{
    public class IndexingInMemoryTableFactory : InMemoryTableFactory
    {
        public IndexingInMemoryTableFactory(ILoggingOptions loggingOptions) : base(loggingOptions)
        {
        }

        public override IInMemoryTable Create(IEntityType entityType)
        {
            var innerTable = base.Create(entityType);
            var uniqueIndexes = entityType.GetIndexes().Where(i => i.IsUnique).ToArray();

            return uniqueIndexes.Any() 
                ? new IndexingInMemoryTable(innerTable, uniqueIndexes) 
                : innerTable;
        }
    }
}