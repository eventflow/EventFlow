.. _ioc-container:

IoC container
=============

EventFlow has a custom minimal IoC container implementation, but before
using EventFlow in a production environment, its recommended to change
to `Autofac <https://autofac.org/>`__ or provide another.

Autofac
-------

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
