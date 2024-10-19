---
title: Configuration
---

# Configuration

EventFlow configuration can be done via the `.Configure(o => {})` method, which is available on the `EventFlowOptions` object.

```csharp
using var serviceCollection = new ServiceCollection()
    // ...
    .AddEventFlow(e => e.Configure(o =>
    {
        o.IsAsynchronousSubscribersEnabled = true;
        o.ThrowSubscriberExceptions = true;
    }))
    // ...
    .BuildServiceProvider();
```

In this example, we enable asynchronous subscribers and configure EventFlow to throw exceptions for subscriber errors. You can customize the configuration options to suit your needs.

For more information on EventFlow configuration, refer to the [EventFlow documentation](https://eventflow.readthedocs.io/en/latest/).
