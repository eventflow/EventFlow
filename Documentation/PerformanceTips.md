# Performance tips for EventFlow

### MSSQL event store
The EventFlow event store for MSSQL has a database schema that is good for
many purposes, but you can optimize it based your usage of EventFlow.

Here are a few suggestions

 * Change the column `AggregateId` to `varchar` instead of `nvarchar` and
   limit the length
