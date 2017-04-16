.. _jobs:

Jobs
====

A job is basically a task that you either don't want to execute in the
current context, on the current server or execute at a later time.
EventFlow provides basic functionality for jobs.

There are areas where you might find jobs very useful, here are some
examples

-  Publish a command at a specific time in the future
-  Transient error handling

.. code-block:: c#

    var jobScheduler = resolver.Resolve<IJobScheduler>();
    var job = PublishCommandJob.Create(new SendEmailCommand(id), resolver);
    await jobScheduler.ScheduleAsync(
      job,
      TimeSpan.FromDays(7),
      CancellationToken.None)
      .ConfigureAwait(false);

In the above example the ``SendEmailCommand`` command will be published
in seven days.


Be careful when using jobs
--------------------------

When working with jobs, you should be aware of the following

-  The default implementation does executes the job *now*, i.e., in the
   current context. To get another behavior, install e.g.
   ``EventFlow.Hangfire`` to get support for scheduled jobs. Read below
   for details on how to configure Hangfire
-  Your jobs should serialize to JSON properly, see the section on
   `value objects <./ValueObjects.md>`__ for more information
-  If you use the provided ``PublishCommandJob``, make sure that your
   commands serialize properly as well


Create your own jobs
--------------------

To create your own jobs, your job merely needs to implement the ``IJob``
interface and be registered in EventFlow.

Here's an example of a job implementing ``IJob``

.. code-block:: c#

    [JobVersion("LogMessage", 1)]
    public class LogMessageJob : IJob
    {
      public LogMessageJob(string message)
      {
        Message = message;
      }

      public string Message { get; }

      public Task ExecuteAsync(
        IResolver resolver,
        CancellationToken cancellationToken)
      {
        var log = resolver.Resolve<ILog>();
        log.Debug(Message);
      }
    }

Note that the ``JobVersion`` attribute specifies the job name and
version to EventFlow and this is how EventFlow distinguishes between the
different job types. This makes it possible for you to reorder your
code, even rename the job type, as long as you keep the same attribute
values its considered the same job in EventFlow. If the attribute is
omitted, the name will be the type name and version will be ``1``.

Here's how the job is registered in EventFlow.

.. code-block:: c#

    var resolver = EventFlowOptions.new
      .AddJobs(typeof(LogMessageJob))
      ...
      .CreateResolver();

Then to schedule the job

.. code-block:: c#

    var jobScheduler = resolver.Resolve<IJobScheduler>();
    var job = new LogMessageJob("Great log message");
    await jobScheduler.ScheduleAsync(
      job,
      TimeSpan.FromDays(7),
      CancellationToken.None)
      .ConfigureAwait(false);


Jobs shipped with EventFlow
---------------------------

EventFlow ships with a few jobs that might be useful. All are located
in the ``EventFlow.Provided.Jobs`` namespace.

* ``ArchiveEventsJob``: Allows scheduling of archiving events
* ``PublishCommandJob``: Allows scheduling of command publishing


Hangfire
--------

To use `Hangfire <http://hangfire.io/>`__ as the job scheduler, install
the NuGet package ``EventFlow.Hangfire`` and configure EventFlow to use
the scheduler like this.

.. code-block:: c#

    var resolver = EventFlowOptions.new
      .UseHangfireJobScheduler() // This line
      ...
      .CreateResolver();

.. NOTE::

    The ``UseHangfireJobScheduler()`` doesn't do any Hangfire
    configuration, but merely registers the proper scheduler in EventFlow.
