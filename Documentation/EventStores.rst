Event stores
============

By default EventFlow uses a in-memory event store. But EventFlow provides
support for alternatives.

- :ref:`Microsoft SQL Server <eventstore-mssql>`


.. _eventstore-mssql:

MSSQL event store
=================

See :ref:`MSSQL setup <setup-mssql>` for details on how to get started
using Microsoft SQL Server in EventFlow.

Configure EventFlow to use MSSQL as event store, simply add the
``UseMssqlEventStore()`` as shown here.

.. code-block:: c#

    IRootResolver rootResolver = EventFlowOptions.New
      ...
      .UseMssqlEventStore()
      ...
      .CreateResolver();


Create and migrate required MSSQL databases
-------------------------------------------

Before you can use the MSSQL event store, the required database and
tables must be created. The database specified in your MSSQL connection
will *not* be automatically created, you have to do this yourself.

To make EventFlow create the required tabeles, execute the following
code.

.. code-block:: c#

    var msSqlDatabaseMigrator = rootResolver.Resolve<IMsSqlDatabaseMigrator>();
    EventFlowEventStoresMsSql.MigrateDatabase(msSqlDatabaseMigrator);

You should do this either on application start or preferably upon
application install or update, e.g., when the web site is installed.

**Note:** If you utilize user permission in your application, then you
need to grant the event writer access to the user defined table type
``eventdatamodel_list_type``. EventFlow uses this type to pass entire
batches of events to the database.
