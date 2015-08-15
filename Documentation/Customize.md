# Customize

When ever EventFlow doesn't meet your needs, e.g. if you want to collect
statistics on each command execution time, you can customize EventFlow.

You have two options

* Decorate an implementation
* Replace an implementation

## Decorating implementations

In the case of collecting statistics, you might want to wrap the existing
`ICommandBus` with a decorator class the can collect statistics on command
execution times.

```csharp
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
```

## Registering new implementations

The more drastic step is to completely replace an implementation. For this
you use the `Register(...)` and related methods on `IServiceRegistration`
instead of the `Decorate(...)` method.

A example of a service that you might be interested in creating your own
custom implementation of is `IAggregateFactory` which handles all aggregate
creation, enabling you to pass additional services to a aggregate upon
creation before events are applied.
