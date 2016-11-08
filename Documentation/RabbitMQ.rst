.. _setup-rabbitmq:

RabbitMQ
========

To setup EventFlow RabbitMQ_ integration, install the NuGet package
``EventFlow.RabbitMQ`` and add this to your EventFlow setup.

.. code-block:: c#

    var uri = new Uri("amqp://localhost");

    var resolver = EventFlowOptions.with
      .PublishToRabbitMq(RabbitMqConfiguration.With(uri))
      ...
      .CreateResolver();


After setting up RabbitMQ support in EventFlow, you can continue to configure it.

- :ref:`Publish all domain events <subscribers-rabbitmq>`


.. _RabbitMQ: https://www.rabbitmq.com/
