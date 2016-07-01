# Sagas

To coordinates messages between bounded contexts and aggregates EventFlow
provides a simple saga system.

* **Saga identity**
* **Saga**
* **Saga locator**
* **Zero or more aggregates**

In this example we will create a basic password reset flow.

1. `UserAggregate` `PasswordResetRequestedEvent`
1. _saga reacts and sends an email_
1. ``

First we need to define the _identity_ of our saga.

```csharp
// You could inherit from Identity<> instead of SingleValueObject<>
public class PasswordResetSagaId : SingleValueObject<string>, ISagaId
{
  public PasswordResetSagaId(string value) : base(value) { }
}
```

Next we need to `ISagaLocator` to tell EventFlow how to translate a given
domain event to a saga identity.

For the sake of simplicity we will add the user email to event metadata of all
events from the user aggregate.

```csharp
public class PasswordResetSagaLocator : ISagaLocator
{
  public Task<ISagaId> LocateSagaAsync(
    IDomainEvent domainEvent,
    CancellationToken cancellationToken)
  {
    var userEmail = domainEvent.Metadata["useremail"];
    var passwordResetSagaId = new PasswordResetSagaId($"passreset-{userEmail}");

    return Task.FromResult<ISagaId>(passwordResetSagaId);
  }
}
```

How you locate a saga based on a domain event, varies from application to
application.

Next we define the saga itself.

```csharp
public class PasswordResetSaga
  : Saga<PasswordResetSaga, PasswordResetSagaId, PasswordResetSagaLocator>,
    ISagaIsStartedBy<UserAggregate, UserId, PasswordResetRequestedEvent>
{
  private int? _verificationCode;

  public Task ProcessAsync(
    IDomainEvent<UserAggregate, UserId, PasswordResetRequestedEvent> domainEvent,
    CancellationToken cancellationToken)
    {
      // Do super secret password reset verification code calculation
      var verificationCode = 1234;

      // Emits an event for this saga
      Emit(new ResetVerificationCodeAssignedEvent(verificationCode));

      // Schedules a command to be sent given that the saga aggregate is
      // committed successfully to the event store
      Publish(new SendPasswordResetCommand(
        domainEvent.AggregateEvent.UserId,
        verificationCode));
    }

  public void Apply(ResetVerificationCodeAssignedEvent e)
  {
    _verificationCode = e.VerificationCode;
  }
}
```
