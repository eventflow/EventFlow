# Commands

Commands are the basic value objects, or models, that represent write operations
that you can perform in your domain.

As an example, one might implement create this command for updating user
passwords.

```csharp
public class UserUpdatePasswordCommand : Command<UserAggregate, UserId>
{
  public Password NewPassword { get; private set; }
  public Password OldPassword { get; private set; }

  public UserUpdatePasswordCommand(
    UserId id,
    Password newPassword,
    Password oldPassword)
    : base(id)
  {
    Username = username;
    Password = password;
  }
}
```

Note that the `Password` class is merely a value object created to hold the
password and do basic validation. Read the article regarding
[value objects](./ValueObjects.md) for more information. Also, you don't
have to use the default EventFlow `Command<,>` implementation, you can create
your own, it merely have to implement the `ICommand<,>` interface.

A command by itself doesn't do anything and will throw an exception if
published. To make a command work, you need to implement one (and only one)
command handler which is responsible for invoking the aggregate.

```csharp
public class UserUpdatePasswordCommandHandler :
  CommandHandler<UserAggregate, UserId, UserUpdatePasswordCommand>
{
  public override Task ExecuteAsync(
    UserAggregate aggregate,
    UserUpdatePasswordCommand command,
    CancellationToken cancellationToken)
  {
    aggregate.UpdatePassword(
      command.OldPassword,
      command.NewPassword);
    return Task.FromResult(0);
  }
}
```

## Ensure idempotency

Detecting duplicate operations can be hard, especially if you have a
distributed application, or simply a web application. Consider the following
simplified scenario.

1. The user wants to change his password
1. The user fills in the "change password form"
1. As user is impatient, or by accident, the user submits the for twice
1. The first web request completes and the password is changed. However, as
   the browser is waiting on the first web request, this result is ignored
1. The second web request throws a domain error as the "old password" doesn't
    match as the current password has already been changed
1. The user is presented with a error on the web page

Handling this is simple, merely ensure that the aggregate is idempotent
is regards to password changes. But instead of implementing this yourself,
EventFlow has support for it and its simple to utilize and is done per
command.

To use the functionality, merely ensure that commands that represent the
same operation has the same `ISourceId` which implements `IIdentity` like
the example blow.

```csharp
public class UserUpdatePasswordCommand : Command<UserAggregate, UserId>
{
  public Password NewPassword { get; private set; }
  public Password OldPassword { get; private set; }

  public UserCreateCommand(
    UserId id,
    ISourceId sourceId,
    Password newPassword,
    Password oldPassword)
    : base(id, sourceId)
  {
    Username = username;
    Password = password;
  }
}
```

Note the use of the other `protected` constructor of `Command<,>` that
takes a `ISourceId` in addition to the aggregate root identity.

If a duplicate command is detected, a `DuplicateOperationException` is thrown.
The application could then ignore the exception or report the problem to the
end user.

The default `ISourceId` history size of the aggregate root, is ten. But it can
be configured using the `SetSourceIdHistory(...)` that must be called from
within the aggregate root constructor.


### Easier ISourceId calculation

Ensuring the correct calculation of the command `ISourceId` can be somewhat
cumbersome, which is why EventFlow provides another base command you can use,
the `DistinctCommand<,>`. By using the `DistinctCommand<,>` you merely have
to implement the `GetSourceIdComponents()` and providing the
`IEnumerable<byte[]>` that makes the command unique. The bytes is used to
create a deterministic GUID to be used as an `ISourceId`.

```csharp
public class UserUpdatePasswordCommand :
  DistinctCommand<UserAggregate, UserId>
{
  public Password NewPassword { get; private set; }
  public Password OldPassword { get; private set; }

  public UserUpdatePasswordCommand(
    UserId id,
    Password newPassword,
    Password oldPassword)
    : base(id)
  {
    Username = username;
    Password = password;
  }

  protected override IEnumerable<byte[]> GetSourceIdComponents()
  {
    yield return NewPassword.GetBytes();
    yield return OldPassword.GetBytes();
  }
}
```

The `GetBytes()` merely returns the `Encoding.UTF8.GetBytes(...)` of the
password.

Its important that you don't use the `GetHashCode()`, as the implementation
is different for e.g. `string` on 32 bit and 64 bit .NET.
