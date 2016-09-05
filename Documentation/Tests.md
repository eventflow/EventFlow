# Test

This document present ideas on how a test suite for an application using
EventFlow might look like.

* Fast in-memory domain tests
* Query hander integration tests

### Fast in-memory domain tests

Testing the entire domain of an application is critical and when working
with EventFlow its relatively easy to setup complex test scenarios for your
domain that can be executed in-memory and in parallel. This enables developers
to have rapid feedback when doing changes to the domain by using tools
like [NCrunch](http://www.ncrunch.net/) or
[dotCover continuous testing](https://www.jetbrains.com/dotcover/).

* Use the default in-memory event store
* Use the in-memory read model store
* Write in-memory query handlers used only for tests

### Query handler integration tests

As query handlers often utilize framework specific functionality to do reads,
e.g. Elasticsearch aggregations or MSSQL indexes, these tend to require a
more complex setup. These setups aren't suited to execute a high volume of
domain tests on-the-fly in the background as the setup and teardown might
require several seconds.
