---
layout: default
title: MongoDB
parent: Integration
nav_order: 2
---

.. _setup-mongodb:

Mongo DB
========

To setup EventFlow Mongo DB, install the NuGet package `EventFlow.MongoDB` and add this to your EventFlow setup.

```csharp
IRootResolver rootResolver = EventFlowOptions.New
  .ConfigureMongoDb(client, "database-name")
  ...
  .CreateResolver();
```

After setting up Mongo DB support in EventFlow, you can continue to configure it.

- :ref:`Event store <eventstore-mongodb>`
- :ref:`Read model store <read-model-mongodb>`
