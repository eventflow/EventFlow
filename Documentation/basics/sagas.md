---
layout: default
title: Sagas
parent: Basics
nav_order: 2
---

# Sagas

EventFlow provides a simple saga system to coordinate messages between 
bounded contexts and aggregates.

-  **Saga identity**
-  **Saga**
-  **Saga locator**
-  **Zero or more aggregates**

This example is based on the chapter "A Saga on Sagas" from the [CQRS Journey](https://msdn.microsoft.com/en-us/library/jj591569.aspx) by Microsoft, in which we want to model the process of placing an order.

1. User sends command `PlaceOrder` to the `OrderAggregate`
2. `OrderAggregate` emits an `OrderCreated` event
3. `OrderSaga` handles `OrderCreated` by sending a
   `MakeReservation` command to the `ReservationAggregate`
4. `ReservationAggregate` emits a `SeatsReserved` event
5. `OrderSaga` handles `SeatsReserved` by sending a `MakePayment`
   command to the `PaymentAggregate`
6. `PaymentAggregate` emits a `PaymentAccepted` event
7. `OrderSaga` handles `PaymentAccepted` by emitting a
   `OrderConfirmed` event with all the details, which via subscribers
   updates the user, the `OrderAggregate` and the
   `ReservationAggregate`

Next, we need an `ISagaLocator` which basically maps domain events to a
saga identity allowing EventFlow to find it in its store.

In our case, we will add the order ID to the event metadata of all events
related to a specific order.

```csharp
public class OrderSagaLocator : ISagaLocator
{
  public Task<ISagaId> LocateSagaAsync(
    IDomainEvent domainEvent,
    CancellationToken cancellationToken)
  {
    var orderId = domainEvent.Metadata["order-id"];
    var orderSagaId = new OrderSagaId($"ordersaga-{orderId}");

    return Task.FromResult<ISagaId>(orderSagaId);
  }
}
```

Alternatively, the order identity could be added to every domain event
emitted from the `OrderAggregate`, `ReservationAggregate`, and
`PaymentAggregate` aggregates that the `OrderSaga` subscribes to,
but this would depend on whether or not the order identity is part of
the ubiquitous language for your domain.

```csharp
public class OrderSaga
  : AggregateSaga<OrderSaga, OrderSagaId, OrderSagaLocator>,
    ISagaIsStartedBy<OrderAggregate, OrderId, OrderCreated>
{
  public Task HandleAsync(
      IDomainEvent<OrderAggregate, OrderId, OrderCreated> domainEvent,
      ISagaContext sagaContext,
      CancellationToken cancellationToken)
  {
    // Update saga state with useful details.
    Emit(new OrderStarted(/*...*/));

    // Make the reservation
    Publish(new MakeReservation(/*...*/));
    
    return Task.FromResult(0);
  }

  public void Apply(OrderStarted e)
  {
    // Update our aggregate state with relevant order details
  }
}
```

!!! attention
    Even though the method for publishing commands is named
    `Publish`, the commands are only published to the command bus
    **after** the aggregate has been successfully committed to the event
    store (just like events). If an unexpected exception is thrown by this
    command publish, it should be handled by a custom implementation of
    `ISagaErrorHandler`.


The next few events and commands are omitted in this example, but at last the
`PaymentAggregate` emits its `PaymentAccepted` event and the saga
completes and emits the final `OrderConfirmed` event.

```csharp
public class OrderSaga
  : AggregateSaga<OrderSaga, OrderSagaId, OrderSagaLocator>,
    // ...
    ISagaHandles<PaymentAggregate, PaymentId, PaymentAccepted>
{
  // ...

  public Task HandleAsync(
    IDomainEvent<PaymentAggregate, PaymentId, PaymentAccepted> domainEvent,
    ISagaContext sagaContext,
    CancellationToken cancellationToken)
  {
    Emit(new OrderConfirmed(/*...*/))
  }

  public void Apply(OrderConfirmed e)
  {
    // As this is the last event, we complete the saga by calling Complete()
    Complete();
  }
}
```

!!! note
    An `AggregateSaga<,,>` is only considered in its `running`
    state if there has been an event and it hasn't been marked as completed
    (by invoking the `protected` `Complete()` method on the
    `AggregateSaga<,,>`).


## Understanding Saga Lifecycle States


Each Saga has an internal ```State``` property that defines how it processes events. This property can have the following values:

- **New**
  - Only events defined using ```ISagaIsStartedBy<>``` can be processed.
- **Running**
  - Events defined using ```ISagaHandles<>``` will be processed.
  - Events defined using ```ISagaIsStartedBy<>``` will also behave the same as ```ISagaHandles<>```.
- **Completed**
  - No events will be processed by the Saga anymore.



## Alternative saga store

By default, EventFlow is configured to use event sourcing and aggregate
roots for the storage of sagas. However, you can implement your own storage
system by implementing `ISagaStore` and registering it.
