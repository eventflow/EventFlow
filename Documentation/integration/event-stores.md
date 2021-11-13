---
layout: default
title: Integration
parent: Basics
nav_order: 2
---


.. _eventstores:

# Event stores

By default EventFlow uses an in-memory event store. But EventFlow provides
support for alternatives.

- :ref:`In-memory <eventstore-inmemory>` (for test)
- :ref:`Microsoft SQL Server <eventstore-mssql>`
- :ref:`Mongo DB <eventstore-mongodb>`
- :ref:`Files <eventstore-files>` (for test)


.. _eventstore-inmemory:

## In-memory

!!! attention
    In-memory event store shouldn't be used for production environments, only for tests.


Using the in-memory event store is easy as it's enabled by default, no need
to do anything.


.. _eventstore-mssql:

## MSSQL event store

See :ref:`MSSQL setup <setup-mssql>` for details on how to get started
using Microsoft SQL Server in EventFlow.

To configure EventFlow to use MSSQL as the event store, simply add the
``UseMssqlEventStore()`` as shown here.

```csharp
IRootResolver rootResolver = EventFlowOptions.New
  ...
  .UseMssqlEventStore()
  ...
  .CreateResolver();
```

### Create and migrate required MSSQL databases

Before you can use the MSSQL event store, the required database and
tables must be created. The database specified in your MSSQL connection
will *not* be automatically created, you have to do this yourself.

To make EventFlow create the required tables, execute the following
code.

```csharp
var msSqlDatabaseMigrator = rootResolver.Resolve<IMsSqlDatabaseMigrator>();
EventFlowEventStoresMsSql.MigrateDatabase(msSqlDatabaseMigrator);
```

You should do this either on application start or preferably upon
application install or update, e.g., when the web site is installed.

!!! attention
    If you utilize user permission in your application, then you
    need to grant the event writer access to the user defined table type
    ``eventdatamodel_list_type``. EventFlow uses this type to pass entire
    batches of events to the database.

.. _eventstore-postgresql:

## PostgreSql event store

Basically all above on MS SQL server store applicable to PostgreSql. See :ref:`MSSQL setup <setup-postgresql>` 
for setup documentation.

.. _eventstore-mongodb:

## Mongo DB

See :ref:`Mongo DB setup <setup-mongodb>` for details on how to get started using Mongo DB in EventFlow.

To configure EventFlow to use Mongo DB as the event store, simply add the ``UseMongoDbEventStore()`` as shown here.

```csharp
IRootResolver rootResolver = EventFlowOptions.New
  ...
  .UseMongoDbEventStore()
  ...
  .CreateResolver();
```

.. _eventstore-files:

## Files

!!! attention
    The Files event store shouldn't be used for production environments, only for tests.


The file based event store is useful if you have a set of events that represents
a certain scenario and would like to create a test that verifies that the domain
handles it correctly.

To use the file based event store, simply invoke ``.UseFilesEventStore`("...")``
with the path containing the files.

```csharp
var storePath = @"c:\eventstore"
var rootResolver = EventFlowOptions.New
    ...
    .UseFilesEventStore(FilesEventStoreConfiguration.Create(storePath))
    ...
    .CreateResolver();
```
