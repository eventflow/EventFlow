---
layout: default
title: Redis
parent: Integration
nav_order: 3
---

Redis
========

### Persistence
In order to use Redis as a primary database for events and read models, some sort of persistence option should be used.
Redis provides several configurable [options](https://redis.io/docs/manual/persistence/).

### Version and modules
In order to use Redis as an event store, Redis version 5 is required in order to use streams.
The read and snapshot store require the Redis Search and JSON modules, which are included in [Redis Stack](https://redis.io/docs/stack/).

### Setup
To setup Redis together with EventFlow, install the NuGet package `EventFlow.Redis` and add `.ConfigureRedis(connectionString)` or `.ConfigureRedis(IConnectionMultiplexer)` to your `EventFlowOptions` configuration.

After the setup, you can configure Redis as your _EventStore_, _ReadStore_ and _SnapshotStore_.

- [Event store](event-stores.md#redis)
- [Read model store](read-stores.md#redis)
