---
layout: default
title: Microsoft SQL Server
parent: Integration
nav_order: 2
---

# Microsoft SQL Server

EventFlow ships with first-class integration for Microsoft SQL Server (MSSQL) across the event store, snapshot store, and read models. This page walks through the required packages, configuration, and operational processes you need to run EventFlow on MSSQL in production.

## Prerequisites

- .NET 8.0 (or the version used by your application) with access to NuGet feeds.
- SQL Server 2017 or later (on-premises or Azure SQL Database) with permissions to create schemas, tables, indexes, and table types.
- An understanding of EventFlow event sourcing concepts such as [aggregates](../basics/aggregates.md) and [read stores](read-stores.md).

## Install the NuGet packages

Add the MSSQL integration package to every project that configures EventFlow.

```powershell
dotnet add package EventFlow.MsSql
```

If you also leverage the generic SQL helpers, ensure `EventFlow.Sql` is referenced; it is already a dependency of the MSSQL package when installed via NuGet.

## Configure EventFlow

All MSSQL components share the same connection configuration. Call `ConfigureMsSql` once before registering the specific stores you need.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddEventFlow(options =>
  {
    options
      .ConfigureMsSql(MsSqlConfiguration
        .New
        .SetConnectionString(@"Server=.\SQLEXPRESS;Database=MyApp;User Id=sa;Password=Pa55w0rd!"))
      .UseMssqlEventStore()
      .UseMsSqlSnapshotStore()
      .UseMssqlReadModel<UserReadModel>()
      .UseMssqlReadModel<UserNicknameReadModel, UserNicknameReadModelLocator>();
  });
}
```

`ConfigureMsSql` registers the `IMsSqlConfiguration` and the database migrator that is reused by the event, snapshot, and read model stores. You can fine-tune the configuration (timeouts, retry counts, schema names) via the fluent helpers on `MsSqlConfiguration`.

## Event store

### Enable the MSSQL event store

The event store replaces the in-memory default by calling `UseMssqlEventStore()`. All aggregates share a single table that stores the full stream history.

```csharp
services.AddEventFlow(o =>
  o.ConfigureMsSql(config)
   .UseMssqlEventStore());
```

### Provision the schema

Before the first aggregate is persisted, run the embedded SQL scripts shipped with EventFlow. This creates the `EventFlow` table, supporting indexes, and the `eventdatamodel_list_type` table type used for batch inserts.

```csharp
using var serviceProvider = services.BuildServiceProvider();
var migrator = serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();
await EventFlowEventStoresMsSql.MigrateDatabaseAsync(migrator, cancellationToken);
```

Run this during deployment or application startup. The migrator is idempotent, so reruns simply ensure the schema is present. If your SQL login does not have `CREATE TYPE` rights, grant them explicitly; otherwise batch appends will fail at runtime.

### Recommended database settings

- Enable [READ_COMMITTED_SNAPSHOT](https://learn.microsoft.com/sql/t-sql/statements/alter-database-transact-sql-set-options) to minimize locking contention under load.
- Monitor transaction log growth—the event store writes append-only batches with explicit transactions.
- Retain the default clustered index unless you have a measured need; the included scripts already optimize the append path.

## Snapshot store

Snapshot persistence reduces load time for long-running aggregates. Enable it with `.UseMsSqlSnapshotStore()` after calling `ConfigureMsSql`.

```csharp
services.AddEventFlow(o =>
  o.ConfigureMsSql(config)
   .UseMsSqlSnapshotStore());
```

Provision the schema using the bundled scripts.

```csharp
var migrator = serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();
await EventFlowSnapshotStoresMsSql.MigrateDatabaseAsync(migrator, cancellationToken);
```

This creates the `EventFlowSnapshots` table and supporting indexes. Snapshots are optional, so call this migrator only when the snapshot store is configured.

## Read model store

MSSQL read models use the generic SQL read store implementation while relying on user-supplied schema scripts. Register each read model with either `.UseMssqlReadModel<TReadModel>()` or the locator overload when IDs are derived from event data.

```csharp
services.AddEventFlow(o =>
  o.ConfigureMsSql(config)
   .UseMssqlReadModel<UserReadModel>()
   .UseMssqlReadModel<UserNicknameReadModel, UserNicknameReadModelLocator>());
```

### Shape your tables

EventFlow does not automatically create read model tables. Instead, generate the DDL once (using `ReadModelSqlGenerator` if you prefer) and deploy it alongside your migrations. The minimal schema requires:

- A table—by convention named `ReadModel-[ClassName]`, or override via `[Table("CustomName")]`.
- A primary key column marked with `[SqlReadModelIdentityColumn]` (type `nvarchar(255)` is typical).
- An integer column decorated with `[SqlReadModelVersionColumn]` to track the sequence number.

Example T-SQL:

```sql
CREATE TABLE [dbo].[ReadModel-UserReadModel]
(
    [Id] NVARCHAR(255) NOT NULL,
    [Version] INT NOT NULL,
    [UserId] NVARCHAR(255) NOT NULL,
    [Username] NVARCHAR(255) NOT NULL,
    CONSTRAINT [PK_ReadModel-UserReadModel] PRIMARY KEY CLUSTERED ([Id])
);
```

Deploy custom scripts with the database migrator. You can embed them in your assembly and run them at startup:

```csharp
await migrator.MigrateDatabaseUsingEmbeddedScriptsAsync(
  typeof(Program).Assembly,
  scriptNamespace: "MyCompany.MyApp.SqlScripts",
  cancellationToken);
```

### Tips for production

- Add covering indexes to match your query patterns; EventFlow only enforces the identity index.
- When read models include JSON or large payloads, use `NVARCHAR(MAX)` and keep the row count lean by projecting separate tables per query.
- The read store honours optimistic concurrency; transient conflicts surface as `ReadModelTemporaryException`. Wrap updates in retry logic where necessary.

## Deployment checklist

- [ ] Run `EventFlowEventStoresMsSql.MigrateDatabaseAsync` in every environment that uses the MSSQL event store.
- [ ] Run `EventFlowSnapshotStoresMsSql.MigrateDatabaseAsync` when snapshots are enabled.
- [ ] Deploy read model DDL scripts alongside your application binaries.
- [ ] Verify connection strings and credentials for background workers that publish commands or process read models.

With these steps in place, your EventFlow application can confidently use Microsoft SQL Server for reliable event sourcing, snapshots, and query projections.
