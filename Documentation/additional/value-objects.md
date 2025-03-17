---
title: Value objects
---

# Event serialization and value objects

One of the important parts of creating an event sourced application is
to ensure that you can always read your event streams. It seems simple
enough, but it is a problem, especially for large applications that
undergo refactoring or domain changes.

The basic idea is to store events in a structure that's easy to access
and migrate if the need should arise. EventFlow, like many other event
sourced systems, stores its events using JSON.

## Making pretty and clean JSON

You might wonder "but, why?", and the reason is somewhat similar to the
reasoning behind [semantic URLs](https://en.wikipedia.org/wiki/Semantic_URL).

Consider the following value object used to validate and contain
usernames in an application.

```csharp
public class Username
{
  public string Value { get; }

  public Username(string value)
  {
    if (string.IsNullOrEmpty(value) || value.Length <= 4)
    {
      throw DomainError.With($"Invalid username '{value}'");
    }

    Value = value;
  }
}
```

First we do some cleanup and re-write it using EventFlows
`SingleValueObject<>`.


```csharp
public class Username : SingleValueObject<string>
{
  public Username(string value) : base(value)
  {
    if (string.IsNullOrEmpty(value) || value.Length <= 4)
    {
      throw DomainError.With($"Invalid username '{value}'");
    }
  }
}
```

Now it looks simple and we might think we can use this value object
directly in our domain events. We could, but the resulting JSON will
look like this.

```json
{
  "Username" : {
    "Value": "my-awesome-username",
  }
}
```

This doesn't look very good. First, that extra property doesn't make it
easier to read and it takes up more space when serializing and
transmitting the event.

In addition, if you use the value object in a web API, people using the
API will need to wrap the properties in their DTOs in a similar way. What
we would like is to modify our serialized event to look like this instead
and still use the value object in our events.

```json
{
  "Username" : "my-awesome-username"
}
```

To do this, we use the custom JSON serializer EventFlow has for single
value objects called `SingleValueObjectConverter` on our `Username`
class like this.

```csharp
[JsonConverter(typeof(SingleValueObjectConverter))] // Only this line added
public class Username : SingleValueObject<string>
{
  public Username(string value) : base(value)
  {
    if (string.IsNullOrEmpty(value) || value.Length <= 4)
    {
      throw DomainError.With($"Invalid username '{value}'");
    }
  }
}
```

The JSON converter understands the single value object and will
serialize and deserialize it correctly.

Using this converter also enables to you replace e.g. raw `string` and
`int` properties with value objects on existing events as they will be
"JSON compatible".

!!! note
    Consider applying this to any classes that inherit from `Identity<>`.
