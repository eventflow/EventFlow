# Getting started

Implementation notes

* Aggregates and events are post fixed with `Aggregate` and
  `Event`, its not required by EventFlow, but it makes it a bit
   easier to read the guide and distinguish the different types.

## Create an aggregate

Let us start by creating a aggregate to represent our users.

```csharp
public class UserAggregate : AggregateRoot<UserAggregate>
{
  public UserAggregate(string id)
    : base(id)
  {
  }
}
```

## Create event

```csharp
public class UserCreatedEvent : AggregateEvent<UserAggregate>
{
  public string Username { get; private set; }
  public string Password { get; private set; }

  public UserCreatedEvent(
    string username,
    string password)
  {
    Username = username;
    Password = password;
  }
}
```

## Update aggregate

```csharp
public class UserAggregate : AggregateRoot<UserAggregate>,
  IEmit<UserCreatedEvent>
{
  public string Username { get; private set; }
  public string Password { get; private set; }

  public UserAggregate(string id)
    : base(id)
  {
  }

  public void Create(
    string username,
    string password)
  {
    if (!IsNew)
    {
      throw DomainError.With("User already created");
    }

    Emit(new UserCreatedEvent(username, password));
  }

  public void Apply(UserCreatedEvent e)
  {
    Username = e.Username;
    Password = e.Password;
  }
}
```

## Create command

```csharp
public class UserCreateCommand : ICommand<UserAggregate>
{
  public string Id { get; private set; }
  public string Username { get; private set; }
  public string Password { get; private set; }

  public UserCreateCommand(
    string id,
    string username,
    string password)
  {
    Id = id;
    Username = username;
    Password = password;
  }

  public Task ExecuteAsync(
    UserAggregate aggregate,
    CancellationToken cancellationToken)
  {
    aggregate.Create(Username, Password);
    return Task.FromResult(0);
  }
}
```
