# KurrentDB persistence integration tests

Run these commands from the repository root for .NET 8:

```powershell
docker compose -p eventflow-kurrentdb-tests -f docker-compose.kurrentdb.yml up -d --wait --force-recreate
dotnet test Source/EventFlow.EventStores.KurrentDB.Tests/EventFlow.EventStores.KurrentDB.Tests.csproj -f net8.0
docker compose -p eventflow-kurrentdb-tests -f docker-compose.kurrentdb.yml down
```

The three in-memory servers use insecure connections for local testing. Stopping
and removing the containers discards their test data. Tests use fresh aggregate
streams and do not reset other streams.

| Test fixture | Default server | Override variable |
| --- | --- | --- |
| Persistence tests | `kurrentdb://localhost:22113?tls=false` | `KURRENTDB_CONNECTION_STRING` |
| Shared event-store suite | `kurrentdb://localhost:22114?tls=false` | `KURRENTDB_SUITE_CONNECTION_STRING` |
| TCP migration (gRPC) | `kurrentdb://localhost:22115?tls=false` | `KURRENTDB_MIGRATION_CONNECTION_STRING` |
| TCP migration (TCP) | `tcp://localhost:21115` | `KURRENTDB_MIGRATION_TCP_URL` |

Use separate dedicated databases: the persistence tests seed raw event shapes,
while the shared suite deserializes `$all` as EventFlow domain events. Start the
shared suite on a fresh database for each run; its first-page assertions assume
that earlier runs have not filled the first 200 events. Run the cleanup command
after tests, including when a test fails.

Append retry tests inject deadline failures before a real server append and after
a successful append whose acknowledgement is lost. They verify idempotent recovery,
a rival command winning the revision, bounded attempts, unchanged batches and
cancellation during backoff. Production transport behaviour is unchanged except
for the explicit per-attempt deadline and retry policy.

Tests cover aggregate appends/reads, optimistic concurrency, global pagination,
checkpoint boundaries, system-stream gaps, cancellation, and soft deletion.
Registration tests cover adapter selection, singleton sharing across scopes,
separate clients per root resolver, and validation without connecting to a server.
The migration fixture references the unchanged TCP adapter only from the test
project. It writes through `EventStore.Client` 5.0.1 and reads/appends through
`KurrentDB.Client` 1.6.0 against the same EventStoreDB 23.10.8 server.

## Global checkpoints

The adapter accepts existing TCP `commit-prepare` checkpoints inclusively: the
record at that location, if present, has not been processed yet. New checkpoints
use `after:commit-prepare` and identify the last scanned record, which is excluded
on resume. Store `NextGlobalPosition` unchanged, including its prefix. These new
checkpoints cannot be passed back to the TCP adapter.

No arithmetic is performed on log offsets. A checkpoint remains usable at the
end of the log and after new events are appended. System streams (names starting
with `$`) are excluded from results but still advance the checkpoint.

Deletion uses the server's soft-delete operation with any expected revision,
matching the TCP adapter. It does not tombstone a stream. Deletion errors are
propagated. Global replay can still return soft-deleted events until scavenging
removes them, matching the existing `$all` behavior.

The test project targets `net8.0` by default. Set `EnableNet48Tests=true` to add the `net48` target, and use the separate TLS profile below. The adapter package always targets both frameworks.

## Registration tests without a server

```powershell
dotnet test Source/EventFlow.EventStores.KurrentDB.Tests/EventFlow.EventStores.KurrentDB.Tests.csproj -f net8.0 --filter FullyQualifiedName~UnitTests
```

## Shared EventFlow event-store suite

`KurrentDBEventStoreTests` inherits all tests from `TestSuiteForEventStore` and
uses the public registration extension. No inherited tests are overridden or
skipped, including the large-gap test. To run only this fixture:

```powershell
dotnet test Source/EventFlow.EventStores.KurrentDB.Tests/EventFlow.EventStores.KurrentDB.Tests.csproj -f net8.0 --filter FullyQualifiedName~KurrentDBEventStoreTests
```

## TCP-to-gRPC migration

The third server uses `eventstore/eventstore:23.10.8-bookworm-slim` with external
TCP enabled. Both migration connection-string overrides must address the same
dedicated database. The two other fixtures continue to use KurrentDB 26.1.1.

```powershell
dotnet test Source/EventFlow.EventStores.KurrentDB.Tests/EventFlow.EventStores.KurrentDB.Tests.csproj -f net8.0 --filter FullyQualifiedName~TcpMigrationTests
```

The tests verify actual TCP-written aggregate replay, continued gRPC writes
readable by TCP, unchanged serialized data and metadata, checkpoints returned by
the legacy adapter (including at the end of the log), cross-client optimistic
concurrency, and paging within a transaction whose events share one commit
position and have different prepare positions.

These tests validate switching client protocols on a compatible server. They do
not perform a database-file upgrade from EventStoreDB 23.10 to KurrentDB 26.1.

## .NET Framework runtime tests on Windows

The `net48` target requires .NET Framework 4.8 or later and Windows HTTP/2
support for client/bidirectional streaming (Windows 11 or later). The KurrentDB
client selects `WinHttpHandler` automatically. Use TLS; the insecure .NET 8
endpoints do not support this transport.

```powershell
docker compose --profile net48 -p eventflow-kurrentdb-tests -f docker-compose.kurrentdb.yml up -d --wait --force-recreate kurrentdb-net48 kurrentdb-suite-net48 eventstore-migration-net48
dotnet test Source/EventFlow.EventStores.KurrentDB.Tests/EventFlow.EventStores.KurrentDB.Tests.csproj -p:EnableNet48Tests=true -f net48
docker compose --profile net48 -p eventflow-kurrentdb-tests -f docker-compose.kurrentdb.yml down
```

| Fixture | TLS port | Override variable |
| --- | --- | --- |
| Persistence | 22116 | `KURRENTDB_NET48_CONNECTION_STRING` |
| Shared suite | 22117 | `KURRENTDB_NET48_SUITE_CONNECTION_STRING` |
| Migration gRPC | 22118 | `KURRENTDB_NET48_MIGRATION_CONNECTION_STRING` |
| Migration TCP | 21118 | `KURRENTDB_NET48_MIGRATION_TCP_URL` |

TLS defaults use `kurrentdb://admin:changeit@localhost:PORT?tlsVerifyCert=false`.
These self-signed development certificates exist only in private container tmpfs
storage. No host certificate-store changes are required. Certificate validation
is disabled only in these local test connection strings; production connections
must use trusted certificates. The migration TCP connection is plaintext on
loopback with development credentials.

`RuntimeTargetTests` checks the actual runtime, target framework attributes of
the loaded adapter/client assemblies, and the Framework HTTP handler. Running
`net48` on a machine with Framework 4.8.1 validates the 4.8-targeted binaries on
that installed runtime, not a separate Framework 4.8-only installation.


Validation on 2026-10-24: all 70 tests passed for the net48 target on .NET
Framework 4.8.1 (runtime 4.8.10008.0), Windows build 26300, over TLS. No tests
were skipped. This includes the unchanged shared EventFlow suite and real TCP
protocol-migration tests.

Validation on 2026-10-26: all 82 tests passed on both net8.0 and net48, with
zero skipped tests, using isolated KurrentDB 26.1.1 and EventStoreDB 23.10.8
databases. The strengthened multi-event lost-acknowledgement cases also passed
on both targets. The new tests simulate failures at the append boundary; they
do not attempt to induce actual network packet loss.
