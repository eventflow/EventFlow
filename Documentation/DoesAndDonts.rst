Does and Don'ts
===============

Whenever creating an application that uses CQRS+ES there are several
things you need to keep in mind to make it easier and minimize the
potential bugs. This guide will give you some details on typical
problems and how EventFlow can help you minimize the risk.

Events
------

Produce clean JSON
^^^^^^^^^^^^^^^^^^

Make sure that when your aggregate events are JSON serialized, they
produce clean JSON as it makes it easier to work with and enable you to
easier deserialize the events in the future.

-  No type information
-  No hints of value objects (see `value objects <ValueObjects.md>`__)

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
`upgrade and version your events <./EventUpgrade.md>`__ for details on
how EventFlow supports you in this.
