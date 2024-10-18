---
layout: default
title: Jobs
parent: Basics
nav_order: 2
---

# Jobs

A job is basically a task that you want to execute outside of the
current context, on another server or at a later time. EventFlow
provides basic functionality for jobs.

There are areas where you might find jobs very useful, here are some
examples

- Publish a command at a specific time in the future
- Transient error handling

```csharp
var jobScheduler = resolver.Resolve<IJobScheduler>();
var job = PublishCommandJob.Create(new SendEmailCommand(id), resolver);
await jobScheduler.ScheduleAsync(
  job,
  TimeSpan.FromDays(7),
  CancellationToken.None)
  .ConfigureAwait(false);
```

In the above example the `SendEmailCommand` command will be published
in seven days.

!!! attention
    When working with jobs, you should be aware of the following

    - The default implementation does executes the job *now* (completely ignoring `runAt`/`delay` parameters) and in the
      current context. To get support for scheduled jobs, inject another implementation of `IJobScheduler`,
      e.g. by  installing `EventFlow.Hangfire` (Read below for details).
    - Your jobs should serialize to JSON properly, see the section on
      [value objects](../additional/value-objects.md) for more information
    - If you use the provided `PublishCommandJob`, make sure that your
      commands serialize properly as well

## Create your own jobs

To create your own jobs, your job merely needs to implement the `IJob`
interface and be registered in EventFlow.

Here's an example of a job implementing `IJob`

```csharp
[JobVersion("LogMessage", 1)]
public class LogMessageJob : IJob
{
  public LogMessageJob(string message)
  {
    Message = message;
  }

  public string Message { get; }

  public Task ExecuteAsync(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
  {
    var log = serviceProvider.GetRequiredService<ILogger<LogMessageJob>>();
    log.LogDebug(Message);
    return Task.CompletedTask;
  }
}
```

Note that the `JobVersion` attribute specifies the job name and
version to EventFlow and this is how EventFlow distinguishes between the
different job types. This makes it possible for you to reorder your
code, even rename the job type. As long as you keep the same attribute
values it is considered the same job in EventFlow. If the attribute is
omitted, the name will be the type name and version will be `1`.

Here's how the job is registered in EventFlow.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddEventFlow(ef =>
  {
    ef.AddJobs(typeof(LogMessageJob));
  });
}
```

Then to schedule the job

```csharp
var jobScheduler = serviceProvider.GetRequiredService<IJobScheduler>();
var job = new LogMessageJob("Great log message");
await jobScheduler.ScheduleAsync(
  job,
  TimeSpan.FromDays(7),
  CancellationToken.None)
  .ConfigureAwait(false);
```

## Hangfire

To use [Hangfire](http://hangfire.io/) as the job scheduler, install
the NuGet package `EventFlow.Hangfire` and configure EventFlow to use
the scheduler like this.

hangfire supports several different storage solutions including Microsoft SQL Server and MongoDB. Use only inMemoryStorage for testing and development.

```csharp
private void RegisterHangfire(IEventFlowOptions eventFlowOptions)
{
    eventFlowOptions.ServiceCollection
        .AddHangfire(c => c.UseInMemoryStorage())
        .AddHangfireServer();
    eventFlowOptions.UseHangfireJobScheduler();
}
```

!!! note
    The `UseHangfireJobScheduler()` doesn't do any Hangfire
    configuration, but merely registers the proper scheduler in EventFlow.
