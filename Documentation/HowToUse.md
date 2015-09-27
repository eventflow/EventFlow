# How to use EventFlow

EventFlow has a lot of functionality and the how much you want to use depends
on several factors.

* How dependent do you want to be on EventFlow, remember EventFlow is
  licensed under the MIT license 
* Does the EventFlow implementation match your application, e.g. how EventFlow
  manages read models might not be optimal in your setup

Here's a list of the different parts of functionality of EventFlow that you
can use in your application.

* **Commands, aggregates and events:** This is considered the _core_
  functionality of EventFlow. You might as well use another framework if you
  don't use this
* **Subscribers:** If you want to react on events emitted from your domain,
  then you need subscribers. These are basically classes that state "in case
  of event A do B"
* **Read models:** EventFlow implements a read model management that fits
  _most_ needs
* **Queries:**
* **Jobs:**
