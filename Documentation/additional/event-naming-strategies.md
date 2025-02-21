---
title: Event Naming Strategies
---

# Event Naming Strategies

Event classes with the same name but in different namespaces can potentially collide, when EventFlow just handles the classname. This can potentially lead to errors as EventFlow doesn't know which event class it should rehydrate and apply to.
In order to avoid this, EventFlow incorporates the concept of event naming strategies that can be used to uniquely identify each event class.


## Built-In Strategies

There are a few naming strategies already available in EventFlow out-of-the-box:

- `DefaultStrategy`: The **default behaviour**, if not overwritten explicitly. This strategy will use the className or (if provided) the value coming from an `[EventVersion]` attribute. This matches the implicit naming strategy from previous EventFlow versions.

- `NamespaceAndNameStrategy`: This strategy uses both the namespace and the class name or (if provided) the value coming from an `[EventVersion]` attribute to uniquely identify an event class.
- `NamespaceAndClassNameStrategy`: This strategy uses both the namespace and the class name to uniquely identify an event class. Any name value coming from an `[EventVersion]` attribute will be ignored for internal naming.

## Custom Strategies

All Event Naming Strategies implement the `IEventNamingStrategy` interface.:

```csharp
public interface IEventNamingStrategy
{
    public string CreateEventName(int version, Type eventType, string name);
}
```

One can come up with their own strategy by implementing this interface and registering it with EventFlow.
