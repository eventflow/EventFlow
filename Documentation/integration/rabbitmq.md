---
layout: default
title: RabbitMQ
parent: Integration
nav_order: 2
---

RabbitMQ
========

To setup EventFlow's [RabbitMQ](https://www.rabbitmq.com/) integration, install the NuGet package
`EventFlow.RabbitMQ` and add this to your EventFlow setup.

```csharp
var uri = new Uri("amqp://localhost");
// ...
.PublishToRabbitMq(RabbitMqConfiguration.With(uri))
// ...
```

After setting up RabbitMQ support in EventFlow, you can continue to configure it.

- [Publish all domain events](../basics/subscribers.md)
