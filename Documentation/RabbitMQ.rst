.. _setup-rabbitmq:

RabbitMQ
========

Enabling EventFlow RabbitMQ_ integration is done by installing the
NuGet package ``EventFlow.RabbitMQ`` and add this to your EventFlow
setup.

.. code-block:: c#

    var uri = new Uri("amqp://localhost");

    var resolver = EventFlowOptions.with
      .PublishToRabbitMq(RabbitMqConfiguration.With(uri))
      ...
      .CreateResolver();

Features provided by ``EventFlow.RabbitMQ``

- :ref:`Publish all domain events <subscribers-rabbitmq>`

.. _RabbitMQ: https://www.rabbitmq.com/