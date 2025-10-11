---
layout: default
title: KurrentDB
parent: Integration
nav_order: 2
---

# KurrentDB

`EventFlow.Kurrent` supplies an event store implementation backed by [KurrentDB](https://kurrent.io/).
Use it when you want EventFlow aggregates to persist to a gRPC-enabled, highly available log store
that is wire-compatible with the EventStoreDB client surface.

The package currently focuses on the **event store**. Read models, snapshot stores, and projections
continue to use the providers documented elsewhere.

## Prerequisites

- A .NET application already wired with `EventFlow`.
- KurrentDB 1.0.0 or newer reachable from your service. The integration tests target the
  `docker.kurrent.io/kurrent-latest/kurrentdb:latest` image.
- Either a connection string (for example `kurrentdb://localhost:2113?tls=false`) or a fully
  configured `KurrentDBClientSettings` instance.
- When TLS is enabled, ensure the process trusts the server certificate and that credentials are
  supplied in the connection string or client settings.

## Install the NuGet package

Add the integration everywhere you configure EventFlow.

```powershell
dotnet add package EventFlow.Kurrent
```

## Configure EventFlow

### Quick start with a connection string

```csharp
// Program.cs / Startup.cs
services.AddEventFlow(options => options
    .UseKurrentEventStore("kurrentdb://localhost:2113?tls=false"));
```

The overload parses the connection string with `KurrentDBClientSettings.Create(...)`, registers a
singleton `KurrentDBClient`, and replaces the default in-memory event persistence with
`KurrentEventPersistence`.

### Advanced configuration

When you need to tune keepalive intervals, certificates, or credentials, build the
`KurrentDBClientSettings` yourself.

```csharp
var settings = new KurrentDBClientSettingsBuilder()
    .WithConnectionString("kurrentdb://kurrent.example.com:2113")
    .WithTlsCertificate("/etc/ssl/certs/kurrent.pem")
    .Build();

services.AddEventFlow(options => options.UseKurrentEventStore(settings));
```

Alternatively, provide a factory that resolves additional dependencies.

```csharp
services.AddEventFlow(options => options.UseKurrentEventStore(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KurrentDBClient>>();
    var settings = KurrentDBClientSettings.Create("kurrentdb://localhost:2113?tls=false");
    return new KurrentDBClient(settings, logger);
}));
```

## Event store behavior

- **Optimistic concurrency** – The first write to a stream uses `StreamState.NoStream`. Subsequent
  appends calculate the expected revision from the first event in the batch. KurrentDB responses
  are surfaced as `OptimisticConcurrencyException` when the revision check fails.
- **Stream positions** – Global positions are serialized as `<commit>-<prepare>`. When resuming a
  subscription or replay, pass the `GlobalPosition` returned by the previous page.
- **Soft deletes** – Calling `IAggregateStore.DeleteAsync` tombstones the stream (`StreamState.Any`).
  Once deleted, KurrentDB prevents recreating the stream with the same ID.
- **Event metadata** – The persisted event type follows
  `{AggregateName}.{EventName}.{EventVersion}`. Data and metadata are encoded as UTF-8 JSON just
  like other EventFlow stores.

## Local development

Launch KurrentDB with Docker and point EventFlow at it.

```powershell
docker run --rm -p 2113:2113 --name kurrentdb \
  -e KURRENTDB_CLUSTER_SIZE=1 \
  -e KURRENTDB_RUN_PROJECTIONS=All \
  -e KURRENTDB_START_STANDARD_PROJECTIONS=true \
  -e KURRENTDB_NODE_PORT=2113 \
  -e KURRENTDB_INSECURE=true \
  -e KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP=true \
  docker.kurrent.io/kurrent-latest/kurrentdb:latest
```

The root of the repository also includes `docker-compose.yml` with a `kurrentdb` service that uses
these defaults. When TLS is disabled (`KURRENTDB_INSECURE=true`), remember to append `?tls=false`
when building your connection string.

## Troubleshooting

- **`OptimisticConcurrencyException`** – Another writer appended to the stream between reads. Retry
  the command or introduce aggregate-level conflict handling.
- **`Invalid global position`** – Persist the `GlobalPosition` returned by `LoadAllCommittedEvents`
  verbatim; do not parse or truncate the `<commit>-<prepare>` format.
- **`ReadState.StreamNotFound`** – The stream has been deleted or never created. Check aggregate
  identifiers and ensure soft deletes are not triggered unexpectedly.
- **TLS handshake failures** – Verify both ends agree on TLS settings and that certificates are
  trusted by the process running EventFlow.

## See also

- [Event stores](event-stores.md#kurrentdb)
- [Read model stores](read-stores.md)
- [Snapshots](../additional/snapshots.md)
- `Source/EventFlow.Kurrent.Tests` for an end-to-end integration fixture
