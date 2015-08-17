# FAQ - frequently asked questions

#### Why isn't there a "global sequence number" on domain events?

While this is easy to support in some event stores like MSSQL, it doesn't
really make sense from a domain perspective. Greg Young also has this to say
on the subject:

> Order is only assured per a handler within an aggregate root
> boundary. There is no assurance of order between handlers or
> between aggregates. Trying to provide those things leads to
> the dark side.
>> [Greg Young](https://groups.yahoo.com/neo/groups/domaindrivendesign/conversations/topics/18453)

#### Why doesn't EventFlow have a unit of work concept?

Short answer, you shouldn't need it. But Mike has a way better answer:

> In the Domain, everything flows in one direction: forward. When something bad
> happens, a correction is applied. The Domain doesn't care about the database
> and UoW is very coupled to the db. In my opinion, it's a pattern which is
> usable only with data access objects, and in probably 99% of the cases you
> won't be needing it. As with the Singleton, there are better ways but
> everything depends on proper domain design.
>> [Mike Mogosanu](http://blog.sapiensworks.com/post/2014/06/04/Unit-Of-Work-is-the-new-Singleton.aspx/)

If your case falls within the 1% case, write an decorator for the `ICommandBus`
that starts a transaction, use MSSQL as event store and make sure your read
models are stored in MSSQL as well.
