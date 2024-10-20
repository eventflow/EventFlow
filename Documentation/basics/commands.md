---
layout: default
title: Commands
parent: Basics
nav_order: 2
---

# Commands

Commands are the basic value objects, or models, that represent the 
*write operations* that you can perform in your domain. As described 
in more detail below a command is the "thing" to be done.  A command 
handler **does** the "thing".

As an example, one might implement this command for updating user
passwords.  

```csharp
public class UserUpdatePasswordCommand : Command<UserAggregate, UserId>
{
  public Password NewPassword { get; }
  public Password OldPassword { get; }

  public UserUpdatePasswordCommand(
    UserId id,
    Password newPassword,
    Password oldPassword)
    : base(id)
  {
    NewPassword = newPassword;
    OldPassword = oldPassword;
  }
}
```

Note that the `Password` class is merely a value object created to
hold the password and do basic validation. Read the article regarding
[value objects](../additional/value-objects.md) for more information. Also, you
don't have to use the default EventFlow `Command<,>` implementation,
you can create your own, it merely has to implement the `ICommand<,>`
interface.

A command by itself doesn't do anything and will throw an exception if
published. To make a command work, you need to implement one (and only
one) command handler which is responsible for invoking the aggregate.

```csharp
public class UserUpdatePasswordCommandHandler : CommandHandler<UserAggregate, UserId, UserUpdatePasswordCommand>
{
  public override Task ExecuteAsync(
    UserAggregate aggregate,
    UserUpdatePasswordCommand command,
    CancellationToken cancellationToken)
  {
    return aggregate.UpdatePasswordAsync(
      command.NewPassword,
      command.OldPassword,
      cancellationToken);
  }
}
```

## Execution results

If the aggregate detects a domain error and wants to abort execution 
and return an error back, then execution results are used. EventFlow
ships with a basic implementation that allows returning *success* or 
*failed* and optionally an error message as shown here.

```csharp
ExecutionResult.Success();
ExecutionResult.Failed();
ExecutionResult.Failed("With some error");
```

However, you can create your own custom execution results to allow
aggregates to e.g. provide detailed validation results. Merely
implement the `IExecutionResult` interface and use the type as
generic arguments on the command and command handler.

!!! tip
    While possible, do not use the execution results as a method of reading
    values from the aggregate, that's what the `IQueryProcessor` and
    read models are for.


## Ensure idempotency

Detecting duplicate operations can be hard, especially if you have a
distributed application, or simply a web application. Consider the
following simplified scenario.

1. The user wants to change his password
2. The user fills in the "change password form"
3. The user submits the form twice, either accidentally or impatiently
4. The first web request completes and the password is changed. However,
   as the browser is waiting on the second web request, this result is
   ignored
5. The second web request throws a domain error as the "old password"
   doesn't match as the current password has already been changed
6. The user is presented with a error on the web page

Handling this is simple, merely ensure that the aggregate is idempotent
in regards to password changes. But instead of implementing this
yourself, EventFlow has support for it that is simple to utilize and is
done per command.

To use the functionality, merely ensure that commands that represent the
same operation have the same `ISourceId` which implements `IIdentity`
like the example below.

```csharp
public class UserUpdatePasswordCommand : Command<UserAggregate, UserId>
{
  public Password NewPassword { get; }
  public Password OldPassword { get; }

  public UserCreateCommand(
    UserId id,
    ISourceId sourceId,
    Password newPassword,
    Password oldPassword)
    : base(id, sourceId)
  {
    NewPassword = newPassword;
    OldPassword = oldPassword;
  }
}
```

Note the use on line 11 of the  `protected` constructor of `Command<,>`
that takes a `ISourceId` in addition to the aggregate root identity.

If a duplicate command is detected, a `DuplicateOperationException` is
thrown. The application could then ignore the exception or report the
problem to the end user.

The default `ISourceId` history size of the aggregate root, is ten.
But it can be configured using the `SetSourceIdHistory(...)` method 
in the aggregate root constructor.

### Easier `ISourceId` calculation

Ensuring the correct calculation of the command `ISourceId` can be
somewhat cumbersome, which is why EventFlow provides another base
command you can use, the `DistinctCommand<,>`. By using the
`DistinctCommand<,>` you merely have to implement the
`GetSourceIdComponents()` and providing the `IEnumerable<byte[]>`
that makes the command unique. The bytes are used to create a
deterministic GUID to be used as an `ISourceId`.


```csharp
public class UserUpdatePasswordCommand :
  DistinctCommand<UserAggregate, UserId>
{
  public Password NewPassword { get; }
  public Password OldPassword { get; }

  public UserUpdatePasswordCommand(
    UserId id,
    Password newPassword,
    Password oldPassword)
    : base(id)
  {
    NewPassword = newPassword;
    OldPassword = oldPassword;
  }

  protected override IEnumerable<byte[]> GetSourceIdComponents()
  {
    yield return NewPassword.GetBytes();
    yield return OldPassword.GetBytes();
  }
}
```

The `GetBytes()` merely returns the `Encoding.UTF8.GetBytes(...)` of
the password.

!!! danger
    Don't use the `GetHashCode()`, as the implementation can be different on 32 bit and 64 bit .NET (e.g. `string`) and can change between .NET versions.
