# Getting started

This guide describes how to get started using EventFlow.

Implementation notes

* Aggregates and events are post fixed with `Aggregate` and
  `Event`, its not required by EventFlow, but it makes it a bit
  easier to read the guide and distinguish the different types
* `.ConfigureAwait(false)` is omitted to make the code easier
  to read

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

Important notes regarding events

* Once have aggregates in your production environment that have
  emitted a event, you should never change it. You can deprecate
  it, but you should never change the data stored in the event store

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
      // If the aggregate isn't new, i.e., events have already
      // been fired for this aggregate, then we have a domain error
      throw DomainError.With("User already created");
    }

    // Everything is okay and thus we emit the event
    Emit(new UserCreatedEvent(username, password));
  }

  public void Apply(UserCreatedEvent e)
  {
    // We must ONLY make state changes in Apply methods as anywhere
    // else will not be persisted
    Username = e.Username;
    Password = e.Password;
  }
}
```

## Create command

Even though it is possible, we are not allowed to call the newly
created `Create` method on our `UserAggregate`. The call must be
made from a command handler, and thus we first create the command.

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
}
```

## Create command handler

Next we create the command handler that invokes the aggregate with the command
arguments.

```csharp
public class UserCreateCommand : ICommand<UserAggregate, UserCreateCommand>
{
  public Task ExecuteAsync(
    UserAggregate aggregate,
    UserCreateCommand command,
    CancellationToken cancellationToken)
  {
    aggregate.Create(command.Username, command.Password);
    return Task.FromResult(0);
  }
}
```


## Create a new user

Now all there is let is to create the user somewhere in your
application by publishing the command.

```csharp
var userId = GetNewRandomUserI();
var username = GetUserEnteredUsername();
var password = GetUserEnteredPassword();

var command = new UserCreateCommand(
  userid,
  username,
  password);

await _commandBus.PublishAsync(command, cancellationToken);
```
