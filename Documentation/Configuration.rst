Configure EventFlow
===================

To get EventFlow up and running, you need an instance of
``IEventFlowOptions`` which is created using the
``EventFlowOptions.New`` static property. From here you use extension
methods and extension methods to setup EventFlow to your needs.

Basic example
-------------

Here's the very minimum that you will need to get EventFlow up and
running with a in-memory configuration.

.. code-block:: c#

    using (var resolver = EventFlowOptions.New
        .AddDefaults(typeof(YouApplication).Assembly)
        .CreateResolver())
    {
      // Do stuff

      // Don't dispose the 'resolver' until application shutdown!
    }

First the default types and services are registered from your
application using the ``AddDefaults(...)`` method.

Versioned types, types that might exist in multiple versions as the
application evolves, are loaded into EventFlow.

-  `Event types <./ValueObjects.md>`__ are loaded into the
   ``IEventDefinitionService``
-  `Command types <./Commands.md>`__ are loaded into the
   ``ICommandDefinitionService``
-  `Job types <./Jobs.md>`__ are loaded into the
   ``IJobDefinitionService``

Services with known interfaces are registered in the built-in IoC
container.

-  Command handlers, i.e., ``ICommandHandler<,,>`` implementations
-  Meta data providers, i.e., ``IMetadataProvider`` implementations
-  Subscribers, i.e., ``ISubscribeSynchronousTo<,,>`` and
   ``ISubscribeSynchronousToAll`` implementations
-  Event upgraders, i.e., ``IEventUpgrader<,>`` implementations
-  Query handlers, i.e., ``IQueryHandler<,>`` implementations

Note that you can use another IoC container, e.g. Autofac using the
``EventFlow.Autofac`` NuGet package. If using this package, you can even
skip the ``CreateResolver()`` call, as EventFlow will automatically
configure itself when the container is ready.

The final step of configuring EventFlow, is to call ``CreateResolver()``
which configures the IoC container and its through this that you can
access e.g. the ``ICommandBus`` which allows you to publish commands.

Configuration methods
---------------------

Note that almost every single one of the configuration methods on
``IEventFlowOptions`` is implemented as extension methods and you will
need to add the appropriate namespace, e.g. ``EventFlow.Extensions``.

Versioned types
~~~~~~~~~~~~~~~

-  ``AddEvents``
-  ``AddCommands``
-  ``AddJobs``

Services
~~~~~~~~

**General** \* ``RegisterServices``, configure other services or
override existing \* ``UseServiceRegistration``, use an alternative IoC
container \* ``UseAutofacContainerBuilder`` in NuGet package
``EventFlow.Autofac`` \* ``RegisterModule``, register modules

**Aggregates** \* ``UseResolverAggregateRootFactory`` \*
``UseAutofacAggregateRootFactory`` in the NuGet package
``EventFlow.Autofac``

-  ``AddAggregateRoots``, add aggregates if using the
   ``UseResolverAggregateRootFactory`` or
   ``UseAutofacAggregateRootFactory`` **Command handlers**
-  ``AddCommandHandlers``

**Subscribers** \* ``AddSubscribers``

**Event upgraders** \* ``AddEventUpgraders``

**Metadata providers** \* ``AddMetadataProviders``

**Event stores** \* ``UseEventStore`` \* ``UseFilesEventStore`` \*
``UseEventStoreEventStore`` in NuGet package
``EventFlow.EventStores.EventStore`` \* ``UseMssqlEventStore`` in NuGet
package ``EventFlow.EventStores.MsSql``

**Read models and stores** \*
``UseReadStoreFor<TReadStore, TReadModel>`` \*
``UseReadStoreFor<TReadStore, TReadModel, TReadModelLocator>>`` \*
``UseInMemoryReadStoreFor<TReadModel>`` \*
``UseInMemoryReadStoreFor<TReadModel, TReadModelLocator>``

**Jobs and schedulers** \* ``UseHangfireJobScheduler`` in the NuGet
package ``EventFlow.Hangfire``

**Queries and handlers** \* ``AddQueryHandlers``
