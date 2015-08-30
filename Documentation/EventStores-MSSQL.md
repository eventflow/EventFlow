# MSSQL event store
To use the MSSQL event store provider you need to install the NuGet
package `EventFlow.EventStores.MsSql`.

Be sure to read the topics on [performance tips](./PerformanceTips.md).

## Configuration

Configure the MSSQL connection and event store as shown here.

```csharp
IRootResolver rootResolver = EventFlowOptions.New
  .ConfigureMsSql(MsSqlConfiguration.New
    .SetConnectionString(@"Server=.\SQLEXPRESS;Database=MyApp;User Id=sa;Password=???"))
  .UseMssqlEventStore()
  ...
  .CreateResolver();
```

## Create and migrate required MSSQL databases

Before you can use the MSSQL event store, the required database
and tables must be created. The database specified in your MSSQL
connection will _not_ be automatically created, you have to do this
yourself.

To make EventFlow create the required tabeles, execute the following code.

```csharp
var msSqlDatabaseMigrator = rootResolver.Resolve<IMsSqlDatabaseMigrator>();
EventFlowEventStoresMsSql.MigrateDatabase(msSqlDatabaseMigrator);
```

You should do this either on application start or preferably upon application
install or update, e.g., when the web site is installed.

**Note:** If you utilize user permission in your application, then you
need to grant the event writer access to the user defined table type
`eventdatamodel_list_type`. EventFlow uses this type to pass entire
batches of events to the database.
