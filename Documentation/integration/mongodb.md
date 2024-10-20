---
layout: default
title: MongoDB
parent: Integration
nav_order: 2
---

Mongo DB
========

To setup EventFlow Mongo DB, install the NuGet package `EventFlow.MongoDB` and add this to your EventFlow setup.

```csharp
// ...
.ConfigureMongoDb(client, "database-name")
// ...
```

After setting up Mongo DB support in EventFlow, you can continue to configure it.

- [Event store](event-stores.md#mongo-db)
- [Read model store](read-stores.md#mongo-db)
