.. _dos-and-donts:

Do's and don'ts
===============

Whenever creating an application that uses CQRS+ES there are several
things you need to keep in mind to make it easier and minimize the
potential bugs. This guide will give you some details on typical
problems and how EventFlow can help you minimize the risk.

Business rules
--------------

Specifications
^^^^^^^^^^^^^^^^^^

`Consider` moving complex business rules to :ref:`specifications <specifications>`.
This eases both readability, testability and re-use.


Events
------

Produce clean JSON
^^^^^^^^^^^^^^^^^^

Make sure that when your aggregate events are JSON serialized, they
produce clean JSON as it makes it easier to work with and enable you to
easier deserialize the events in the future.

-  No type information
-  No hints of value objects (see :ref:`value objects <value-objects>`)

Here's an example of good clean event JSON produced from a create user
event.

.. code:: json

    {
      "Username": "root",
      "PasswordHash": "1234567890ABCDEF",
      "EMail": "root@example.org",
    }

Keep old event types
^^^^^^^^^^^^^^^^^^^^

Keep in mind, that you need to keep the event types in your code for as
long as these events are in the event source, which in most cases are
*forever* as storage is cheap and information, i.e., your domain events,
are expensive.

However, you should still clean your code, have a look at how you can
:ref:`upgrade and version your events <event-upgrade>` for details on
how EventFlow supports you in this.


Subscribers and out of order events
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Be very careful if aggregates emits multiple events for a single command,
subscribers will almost certainly
:ref:`receive these out of order <out-of-order-event-subscribers>`.