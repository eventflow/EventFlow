using EventFlow.Core;

namespace EventFlow.Redis.SnapshotStore;

public class SnapshotId : Identity<SnapshotId>
{
    public SnapshotId(string value) : base(value)
    {
    }
}