# KurrentDB event persistence for EventFlow 0.x

This adapter uses `KurrentDB.Client` 1.6.0 and targets .NET Framework 4.8 and
.NET 8.0. It is separate from the existing EventStore TCP adapter.

```csharp
using EventFlow;
using EventFlow.EventStores.KurrentDB.Extensions;

using (var resolver = EventFlowOptions.New
    .UseKurrentDBEventStore(connectionString)
    .CreateResolver())
{
    // Resolve EventFlow services here.
}
```

Use a KurrentDB gRPC connection string rather than the TCP adapter's `tcp://`
connection string. For the local integration-test server, use
`kurrentdb://localhost:22113?tls=false`. Configure TLS and credentials for your
server in the connection string.

Registration validates the connection string and registers `KurrentDBClient` as
a singleton factory. Each root resolver creates and owns its own client; child
scopes share it. Dispose the root resolver to release the client. Registration
and service resolution do not wait for a network connection.

Existing aggregate stream names, event type names, UTF-8 data/metadata, and
one-based aggregate sequence numbers are preserved. Writes enforce optimistic
concurrency and translate conflicts to EventFlow's `OptimisticConcurrencyException`.

Append deadline failures are retried up to five attempts with a 30-second deadline
per attempt and cancellable delays of 50, 100, 150 and 200 milliseconds. A retry
reuses the exact event batch, storage IDs and expected revision: if the first
append committed but its acknowledgement was lost, the repeated append is
idempotent. Separate CommitEventsAsync calls still create fresh storage IDs so
competing commands cannot be mistaken for the same write. Other errors are not
retried by the adapter; revision conflicts remain OptimisticConcurrencyException.
The final deadline error is propagated if retries are exhausted. Cancellation
stops the retry sequence. An exhausted timeout still has an unknown write outcome.
Existing TCP global checkpoints (`commit-prepare`) are accepted inclusively.
New checkpoints use `after:commit-prepare` and resume after the last scanned
record. Store `NextGlobalPosition` unchanged. New checkpoints cannot be consumed
by the TCP adapter. Global reads exclude streams whose names start with `$`.

Deletion soft-deletes streams. Global replay can still return unscavenged events
from deleted streams, as with the TCP adapter.

The `net48` binaries passed all 82 tests on .NET Framework 4.8.1
(runtime 4.8.10008.0), Windows build 26300, using TLS and `WinHttpHandler`.
This validates that installed runtime; a Framework 4.8-only installation has not
been tested. Framework applications require TLS and Windows HTTP/2 support for
client/bidirectional streaming (Windows 11 or later). Windows Server 2019/2022
has streaming restrictions, so do not assume append support from read support.
See the adjacent test project's README for separate Framework and .NET 8 setups.

Protocol migration tests exercise actual streams and checkpoints written by the
unchanged legacy TCP adapter against EventStoreDB 23.10.8. They verify aggregate
replay, continued writes, concurrency, and transaction-boundary pagination. They
do not validate a server storage upgrade; see the test project's README.
