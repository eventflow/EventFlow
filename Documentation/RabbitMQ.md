# RabbitMQ

Configuring EventFlow to publish events to [RabbitMQ](http://www.rabbitmq.com/)
is simple, just install the NuGet package `EventFlow.RabbitMQ` and add this to
your EventFlow setup.

```csharp
var uri = new Uri("amqp://localhost");

var resolver = EventFlowOptions.with
  .PublishToRabbitMq(RabbitMqConfiguration.With(uri))
  ...
  .CreateResolver();
```

Events are published to a exchange named `eventflow` with routing keys in the
following format.

```
eventflow.domainevent.[Aggregate name].[Event name].[Event version]
```

Which will be the following for an event named `CreateUser` version `1` for the
`MyUserAggregate`.

```
eventflow.domainevent.my-user.create-user.1
```

Note the lowercasing and adding of `-` whenever there's a capital letter.

All the above is the default behavior, if you don't like it replace e.g. the
service `IRabbitMqMessageFactory` to customize what routing key or exchange to
use. Have a look at how [EventFlow](https://github.com/rasmus/EventFlow) has
done its implementation to get started.
