---
layout: default
title: PostgreSQL
parent: Integration
nav_order: 2
---

## PostgreSql

To setup EventFlow PostgreSql integration, install the NuGet
package [EventFlow.PostgreSql](https://www.nuget.org/packages/EventFlow.PostgreSql) and add this to your EventFlow setup.

```csharp
IRootResolver rootResolver = EventFlowOptions.New
  .ConfigurePostgreSql(PostgreSqlConfiguration.New
    .SetConnectionString(@"User ID=me;Password=???;Host=localhost;Port=5432;Database=MyApp"))
  .UsePostgreSqlEventStore()
  .UsePostgreSqlSnapshotStore()
  .UsePostgreSqlReadModel<UserReadModel>()
  .UsePostgreSqlReadModel<UserNicknameReadModel,UserNicknameReadModelLocator>()
  // ...
  .CreateResolver();
```

This code block configures Eventflow to store events, snapshots and read models in PostgreSql. It's not mandatory, you 
can mix and match, i.e. storing events in PostgreSql, read models in Elastic search and don't using snapshots at all.

- Event store. One big table `EventFlow` for all events for all aggregates.
- Read model store. Table `ReadModel-[ClassName]` per read model type. 
- Snapshot store. One big table `EventFlowSnapshots` for all aggregates.
