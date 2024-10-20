---
layout: default
title: Queries
parent: Basics
nav_order: 2
---

# Queries

Creating queries in EventFlow is simple.

First, create a value object that contains the data required for the
query. In this example, we want to search for users based on their
username.

```csharp
public class GetUserByUsernameQuery : IQuery<User>
{
  public string Username { get; }

  public GetUserByUsernameQuery(string username)
  {
    Username = username;
  }
}
```

Next, create a query handler that implements how the query is processed.

```csharp
public class GetUserByUsernameQueryHandler :
  IQueryHandler<GetUserByUsernameQuery, User>
{
  private readonly IUserReadModelRepository _userReadModelRepository;

  public GetUserByUsernameQueryHandler(
    IUserReadModelRepository userReadModelRepository)
  {
    _userReadModelRepository = userReadModelRepository;
  }

  public Task<User> ExecuteQueryAsync(
    GetUserByUsernameQuery query,
    CancellationToken cancellationToken)
  {
    return _userReadModelRepository.GetByUsernameAsync(
      query.Username,
      cancellationToken);
  }
}
```

The last step is to register the query handler in EventFlow. Here we show
the simple, but cumbersome version. You should use one of the overloads
that scans an entire assembly.

```csharp
//...
.AddQueryHandler<GetUserByUsernameQueryHandler, GetUserByUsernameQuery, User>();
//...
```

Then, to use the query in your application, you need a reference
to the `IQueryProcessor`, which in our case is stored in the
`_queryProcessor` field.

```csharp
var user = await _queryProcessor.ProcessAsync(
  new GetUserByUsernameQuery("root"),
  cancellationToken);
```

## Queries shipped with EventFlow

-  `ReadModelByIdQuery<TReadModel>`: Supported by both the in-memory
   and MSSQL read model stores automatically as soon as you define the
   read model use using the EventFlow options for that store.
-  `InMemoryQuery<TReadModel>`: Takes a `Predicate<TReadModel>` and
   returns `IEnumerable<TReadModel>`, making it possible to search all
   of your in-memory read models based on any predicate.
