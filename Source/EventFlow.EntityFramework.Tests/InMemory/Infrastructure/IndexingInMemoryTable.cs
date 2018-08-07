using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Storage.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;

namespace EventFlow.EntityFramework.Tests.InMemory.Infrastructure
{
    public class IndexingInMemoryTable : IInMemoryTable
    {
        private readonly IIndex[] _indexDefinitions;
        private readonly HashSet<IndexEntry>[] _indexes;
        private readonly IInMemoryTable _innerTable;

        public IndexingInMemoryTable(IInMemoryTable innerTable, IIndex[] indexDefinitions)
        {
            _innerTable = innerTable;
            _indexDefinitions = indexDefinitions;
            _indexes = _indexDefinitions.Select(i => new HashSet<IndexEntry>()).ToArray();
        }

        public IReadOnlyList<object[]> SnapshotRows()
        {
            return _innerTable.SnapshotRows();
        }

        public void Create(IUpdateEntry entry)
        {
            var indexEntries = _indexDefinitions
                .Select(d => d.Properties.Select(entry.GetCurrentValue).ToArray())
                .Select(values => new IndexEntry(values))
                .ToArray();

            if (indexEntries.Select((item, i) => _indexes[i].Contains(item)).Any(contains => contains))
                throw new DbUpdateException("Error while updating.", new Exception("Unique constraint violated."));

            _innerTable.Create(entry);

            indexEntries.Select((item, i) => _indexes[i].Add(item)).ToArray();
        }

        public void Delete(IUpdateEntry entry)
        {
            _innerTable.Delete(entry);
        }

        public void Update(IUpdateEntry entry)
        {
            _innerTable.Update(entry);
        }

        private struct IndexEntry
        {
            private readonly object[] _values;

            public IndexEntry(object[] values)
            {
                _values = values;
            }

            public bool Equals(IndexEntry other)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(_values, other._values);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is IndexEntry entry && Equals(entry);
            }

            public override int GetHashCode()
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(_values);
            }
        }
    }
}
