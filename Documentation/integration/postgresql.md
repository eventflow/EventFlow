---
layout: default
title: PostgreSQL
parent: Integration
nav_order: 2
---

## PostgreSQL

To setup EventFlow PostgreSQL integration, install the NuGet
package [EventFlow.PostgreSql](https://www.nuget.org/packages/EventFlow.PostgreSql) and add this to your EventFlow setup.

```csharp
// ...
.ConfigurePostgreSql(PostgreSqlConfiguration.New
  .SetConnectionString(@"User ID=me;Password=???;Host=localhost;Port=5432;Database=MyApp"))
.UsePostgreSqlEventStore()
.UsePostgreSqlSnapshotStore()
.UsePostgreSqlReadModel<UserReadModel>()
.UsePostgreSqlReadModel<UserNicknameReadModel,UserNicknameReadModelLocator>()
// ...
```

This code block configures EventFlow to store events, snapshots and read models in PostgreSQL. It's not mandatory, you 
can mix and match, i.e. storing events in PostgreSQL, read models in Elastic search and don't using snapshots at all.

- Event store. One big table `EventFlow` for all events for all aggregates.
- Read model store. Table `ReadModel-[ClassName]` per read model type. 
- Snapshot store. One big table `EventFlowSnapshots` for all aggregates.
