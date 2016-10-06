Customize
=========

When ever EventFlow doesn't meet your needs, e.g. if you want to collect
statistics on each command execution time, you can customize EventFlow.

Basically EventFlow relies on an IoC container to allow developers to
customize the different parts of EventFlow.

Note: Read the section "Changing IoC container" for details on how to
change the IoC container used if you have specific needs like e.g.
integrating EventFlow into an Owin application.

You have two options for when you want to customize EventFlow

-  Decorate an implementation
-  Replace an implementation

Decorating implementations
--------------------------

In the case of collecting statistics, you might want to wrap the
existing ``ICommandBus`` with a decorator class the can collect
statistics on command execution times.

.. code-block:: c#

    void ConfigureEventFlow()
    {
      var resolver = EventFlowOptions.new
        .RegisterServices(DecorateCommandBus)
        ...
        .CreateResolver();
    }

    void DecorateCommandBus(IServiceRegistration sr)
    {
      sr.Decorate<ICommandBus>((r, cb) => new StatsCommandBus(sb));
    }

    class StatsCommandBus : ICommandBus
    {
      private readonly _internalCommandBus;

      public StatsCommandBus(ICommandBus commandBus)
      {
        _internalCommandBus = commandBus;
      }

      // Here follow implementations of ICommandBus that call the
      // internal command bus and logs statistics
      ...
    }

Registering new implementations
-------------------------------

The more drastic step is to completely replace an implementation. For
this you use the ``Register(...)`` and related methods on
``IServiceRegistration`` instead of the ``Decorate(...)`` method.

A example of a service that you might be interested in creating your own
custom implementation of is ``IAggregateFactory`` which handles all
aggregate creation, enabling you to pass additional services to a
aggregate upon creation before events are applied.

Changing IoC container
----------------------

EventFlow provides the NuGet package ``EventFlow.Autofac`` that allows
you to set the internal ``ContainerBuilder`` used during EventFlow
initialization.

Pass the ``ContainerBuilder`` to EventFlow and call
``CreateContainer()`` when configuration is done to create the
container.

.. code-block:: c#

    var containerBuilder = new ContainerBuilder();

    var container = EventFlowOptions.With
      .UseAutofacContainerBuilder(containerBuilder) // Must be the first line!
      ...
      .CreateContainer();

Maybe call ``UseAutofacAggregateRootFactory()`` just before the
``CreateContainer()`` to use the Autofac aggregate root factory.
