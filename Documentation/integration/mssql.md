---
layout: default
title: Microsoft SQL Server
parent: Integration
nav_order: 2
---

Microsoft SQL Server
====================

To setup EventFlow Microsoft SQL Server integration, install the NuGet
package `EventFlow.MsSql` and add this to your EventFlow setup.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddEventFlow(ef =>
  {
    ef.ConfigureMsSql(MsSqlConfiguration.New
      .SetConnectionString(@"Server=.\SQLEXPRESS;Database=MyApp;User Id=sa;Password=???"))
    .UseMssqlEventStore();
  });
}
```

After setting up Microsoft SQL Server support in EventFlow, you can
continue to configure it.

- [Event store](event-stores.md#mongo-db)
- [Read model store](read-stores.md#mongo-db)
