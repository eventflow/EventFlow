---
layout: default
title: Read stores
parent: Integration
nav_order: 2
---

# Read model stores

In order to create query handlers that perform and enable them to search
across multiple fields, read models or projections are used.

To get started, you can use the built-in in-memory read model store, but
EventFlow supports a few others as well.

- [In-memory](#in-memory)
- [Microsoft SQL Server](#microsoft-sql-server)
- [Elasticsearch](#elasticsearch)
- [MongoDB](#mongo-db)
- [Redis](#redis)

## Creating read models

Read models are a flattened view of a subset or all aggregate domain
events created specifically for efficient queries.

Here's a simple example of how a read model for doing searches for
usernames could look. The read model handles the `UserCreated` domain
event to get the username and user ID.

```csharp
public class UserReadModel : IReadModel,
  IAmReadModelFor<UserAggregate, UserId, UserCreated>
{
  public string UserId { get; set; }
  public string Username { get; set; }

  public Task ApplyAsync(
    IReadModelContext context,
    IDomainEvent<UserAggregate, UserId, UserCreated> domainEvent,
    CancellationToken cancellationToken)
  {
    UserId = domainEvent.AggregateIdentity.Value;
    Username = domainEvent.AggregateEvent.Username.Value;
    return Task.CompletedTask;
  }
}
```

The read model applies all `UserCreated` events and thereby merely saves
the latest value instead of the entire history, which makes it much easier to
store in an efficient manner.

## Read model locators

Typically, the ID of read models is the aggregate identity, but
sometimes this isn't the case. Here are some examples.

- Items from a collection on the aggregate root
- Deterministic ID created from event data
- Entity within the aggregate

To create read models in these cases, use the EventFlow concept of read
model locators, which is basically a mapping from a domain event to a
read model ID.

As an example, consider if we could add several nicknames to a user. We
might have a domain event called `UserNicknameAdded` similar to this.

```csharp
public class UserNicknameAdded : AggregateEvent<UserAggregate, UserId>
{
  public Nickname Nickname { get; set; }
}
```

We could then create a read model locator that would return the ID for
each nickname we add via the event like this.

```csharp
public class UserNicknameReadModelLocator : IReadModelLocator
{
  public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
  {
    var userNicknameAdded = domainEvent as
      IDomainEvent<UserAggregate, UserId, UserNicknameAdded>;
    if (userNicknameAdded == null)
    {
      yield break;
    }

    yield return userNicknameAdded.Nickname.Id;
  }
}
```

And then use a read model similar to this that represents each nickname.

```csharp
public class UserNicknameReadModel : IReadModel,
  IAmReadModelFor<UserAggregate, UserId, UserNicknameAdded>
{
  public string UserId { get; set; }
  public string Nickname { get; set; }

  public Task ApplyAsync(
    IReadModelContext context,
    IDomainEvent<UserAggregate, UserId, UserNicknameAdded> domainEvent,
    CancellationToken cancellationToken)
  {
    UserId = domainEvent.AggregateIdentity.Value;
    Nickname = domainEvent.AggregateEvent.Nickname.Value;
    return Task.CompletedTask;
  }
}
```

You may need to assign the ID of your read model from a batch of nicknames assigned
on the creation event of the username. You would then read the assigned read model ID
acquired from the locator using the 'context' field:

```csharp
public class UserNicknameReadModel : IReadModel,
  IAmReadModelFor<UserAggregate, UserId, UserCreatedEvent>
{
  public string Id { get; set; }
  public string UserId { get; set; }
  public string Nickname { get; set; }

  public Task ApplyAsync(
    IReadModelContext context,
    IDomainEvent<UserAggregate, UserId, UserCreatedEvent> domainEvent,
    CancellationToken cancellationToken)
  {
    var id = context.ReadModelId;
    UserId = domainEvent.AggregateIdentity.Value;        
    var nickname = domainEvent.AggregateEvent.Nicknames.Single(n => n.Id == id);
    
    Id = nickname.Id;
    Nickname = nickname.Nickname;
    return Task.CompletedTask;
  }
}
```

We could then use this nickname read model to query all the nicknames
for a given user by searching for read models that have a specific
`UserId`.

## Read store implementations

EventFlow has built-in support for several different read model stores.

### In-memory

!!! attention
    In-memory event store shouldn't be used for production environments, only for tests.

The in-memory read store is easy to use and easy to configure. All read
models are stored in-memory, so if EventFlow is restarted, all read
models are lost.

To configure the in-memory read model store, simply call
`UseInMemoryReadStoreFor<>` or `UseInMemoryReadStoreFor<,>` with
your read model as the generic argument.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddEventFlow(ef =>
  {
    ef.UseInMemoryReadStoreFor<UserReadModel>();
    ef.UseInMemoryReadStoreFor<UserNicknameReadModel, UserNicknameReadModelLocator>();
  });
}
```

### Microsoft SQL Server

To configure the MSSQL read model store, simply call
`UseMssqlReadModel<>` or `UseMssqlReadModel<,>` with your read model
as the generic argument.

```csharp
// ...
.UseMssqlReadModel<UserReadModel>()
.UseMssqlReadModel<UserNicknameReadModel,UserNicknameReadModelLocator>()
// ...
```

By convention, EventFlow uses the table named `ReadModel-[CLASS NAME]`
as the table to store the read model rows in. If you need to change
this, use the `Table` from the
`System.ComponentModel.DataAnnotations.Schema` namespace. So in the
above example, the read model `UserReadModel` would be stored in a
table called `ReadModel-UserReadModel` unless stated otherwise.

To allow EventFlow to find the read models stored, a single column is
required to have the `SqlReadModelIdentityColumn` attribute. This
will be used to store the read model ID.

You should also create an `int` column that has the
`SqlReadModelVersionColumn` attribute to tell EventFlow which column
the read model version is stored in.

!!! attention
    EventFlow expects the read model to exist, and thus any
    maintenance of the database schema for the read models must be handled
    before EventFlow is initialized. Or, at least before the read models are
    used in EventFlow.

### Elasticsearch

To configure the
`Elasticsearch <https://www.elastic.co/products/elasticsearch>`__ read
model store, simply call ``UseElasticsearchReadModel<>`` or
`UseElasticsearchReadModel<,>` with your read model as the generic
argument.

```csharp
// ...
.ConfigureElasticsearch(new Uri("http://localhost:9200/"))
// ...
.UseElasticsearchReadModel<UserReadModel>()
.UseElasticsearchReadModel<UserNicknameReadModel,UserNicknameReadModelLocator>()
// ...
```

Overloads of `ConfigureElasticsearch(...)` are available for
alternative Elasticsearch configurations.

!!! attention
    Make sure to create any mapping the read model requires in Elasticsearch
    *before* using the read model in EventFlow.

If EventFlow receives a request to *purge* a specific read model, it does it
by deleting the index. This means that a separate index should be created for
each read model.

If you want to control the index a specific read model is stored in,
create an implementation of `IReadModelDescriptionProvider` and
register it in the service collection.

### MongoDB

To configure the MongoDB read model store, call `UseMongoDbReadModel<>` or
`UseMongoDbReadModel<,>` with your read model as the generic
argument.

```csharp
// ...
.UseMongoDbReadModel<UserReadModel>()
.UseMongoDbReadModel<UserNicknameReadModel,UserNicknameReadModelLocator>()
// ...
```

### Redis

To configure Redis as a read model store, call `UseRedisReadModel<>` or
`UseRedisReadModel<,>` with your read model and optionally the _ReadModelLocator_ as the generic
argument.

In order to use Redis as your read model store, you need to enable the Redis Search and Redis JSON modules, both of which are included in [Redis Stack](https://redis.io/docs/stack/get-started/install/docker/).

```csharp
// ...
.UseRedisReadModel<UserReadModel>()
.UseRedisReadModel<UserNicknameReadModel,UserNicknameReadModelLocator>()
// ...
```

`EventFlow.Redis` uses [Redis OM](https://github.com/redis/redis-om-dotnet) to provide a LINQ-like querying experience. 
Keep in mind that in order to query a read model by a field other than the ID, you have to add the `[Indexed]` attribute to the field. For more information, check the Redis OM documentation.
