# MSSQL event store
To use the MSSQL event store provider you need to install the NuGet
package `EventFlow.ReadStores.MsSql` and configure the connection
string as shown here.

```csharp
var resolver = EventFlowOptions.New
  .ConfigureMsSql(MsSqlConfiguration.New
    .SetConnectionString(@"Server=SQLEXPRESS;User Id:sa;Password=?"))
  .UseEventStore<MsSqlEventStore>()
  ...
  .CreateResolver();
```
