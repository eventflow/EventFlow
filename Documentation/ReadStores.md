# Read model stores

In order to create query handlers that perform and enable them search across
multiple fields, read models or projects are used.

Read models are a flatten views of a subset or all aggregate domain events
created specifically for efficient queries.

Here's a simple example of how a read models for doing searches for usernames
could look. The read model handles the `UserCreated` domain event event to get
the username and user ID.

```csharp
public class UserReadModel : IReadModel,
  IAmReadModelFor<UserAggregate, UserId, UserCreated>
{
  public string UserId { get; set; }
  public string Username { get; set; }

  public void Apply(
    IReadModelContext context,
    IDomainEvent<UserAggregate, UserId, UserCreated> domainEvent)
  {
    UserId = domainEvent.AggregateIdentity.Value;
    Username = domainEvent.AggregateEvent.Username.Value;
  }
}
```

## Read model locators

Typically the ID of read models are the aggregate identity, but sometimes this
isn't the case. Here are some examples.

- Items from a collection on the aggregate root
- Deterministic ID created from event data
- Entity within the aggregate root

To create read models in these cases, use the EventFlow concept of read model
locators, which is basically a mapping from a domain event to a read model ID.

As an example, consider if we could add several nicknames to a user. We might
have a domain event called `UserNicknamesAdded` similar to this.

```csharp
public class UserNicknamesAdded : AggregateEvent<UserAggregate, UserId>
{
  public IReadOnlyCollection<Nickname> Nicknames { get; set; }
}
```

We could then create a read model locator that would return the ID for each
nickname in the `Nicknames` collection by implementing it like this. 

```csharp
public class UserNicknameReadModelLocator : IReadModelLocator
{
  public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
  {
    var userNicknamesAdded = domainEvent as
      IDomainEvent<UserAggregate, UserId, UserNicknamesAdded>;
    return userNicknamesAdded == null
      ? Enumerable.Empty<string>()
      : userNicknamesAdded.Nicknames.Select(n => n.Id);
  }
}
```

## Read store implementations

EventFlow has built-in support for several different read model stores.

### In-memory


### Elasticsearch

Configuring EventFlow to use
[Elasticsearch](https://www.elastic.co/products/elasticsearch) as a store for
read models is done in steps.

1. Configure Elasticsearch connection in EventFlow
1. Configure your Elasticsearch read models in EventFlow

Given you have defined a read model class named `MyElasticsearchReadModel`, the
above will look like this.

```csharp
var resolver = EventFlowOptions.New
  .ConfigureElasticsearch(new Uri("http://localhost:9200/"))
  .UseElasticsearchReadModel<MyElasticsearchReadModel>()
  ...
  .CreateResolver();
```

Overloads of `ConfigureElasticsearch(...)` is available for alternative
Elasticsearch configuration.

EventFlow makes assumptions regarding how you use Elasticsearch to store read
models.

* The host application of EventFlow is responsible for creating correct
  Elasticsearch type mapping for any indexes by creating index templates

If you want to control the index a specific read model is stored in, create
create an implementation of `IReadModelDescriptionProvider` and register it
in the [EventFlow IoC](./Customize.md).

### MSSQL
