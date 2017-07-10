.. _configuration:

Configuration
=============

EventFlow can be configured by invoking ``eventFlowOptions.Configure(c => ...)```, or
by providing a custom implementation of ``IEventFlowConfiguration``.

Each configuration is described below. The default values should be good enough
for most production setups.

.. literalinclude:: ../Source/EventFlow/Configuration/IEventFlowConfiguration.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 28-65
