---
layout: default
title: RabbitMQ
parent: Integration
nav_order: 2
---

.. _setup-rabbitmq:

RabbitMQ
========

To setup EventFlow's RabbitMQ_ integration, install the NuGet package
`EventFlow.RabbitMQ` and add this to your EventFlow setup.

```csharp
var uri = new Uri("amqp://localhost");

var resolver = EventFlowOptions.with
  .PublishToRabbitMq(RabbitMqConfiguration.With(uri))
  // ...
  .CreateResolver();
```

After setting up RabbitMQ support in EventFlow, you can continue to configure it.

- :ref:`Publish all domain events <subscribers-rabbitmq>`


.. _RabbitMQ: https://www.rabbitmq.com/
