# Read model stores

In order to create query handlers that perform and enable them search across
multiple fields, read models or projects are used.

Read models are a flatten views of a subset or all aggregate domain events
created specifically for efficient queries.

```csharp
public class UserReadModel : IReadModel,

{
}
```

## Read store implementations

EventFlow has built-in support for several different read model stores.

### In-memory


### Elasticsearch

Configuring EventFlow to use
[Elasticsearch](https://www.elastic.co/products/elasticsearch) as a store for
read models is done in steps.

1. Configure Elasticsearch connection in EventFlow
1. Configure your Elasticsearch read models in EventFlow

Given you have defined a read model class named `MyElasticsearchReadModel`, the
above will look like this.

```csharp
var resolver = EventFlowOptions.New
  .ConfigureElasticsearch(new Uri("http://localhost:9200/"))
  .UseElasticsearchReadModel<MyElasticsearchReadModel>()
  ...
  .CreateResolver();
```

Overloads of `ConfigureElasticsearch(...)` is available for alternative
Elasticsearch configuration.

EventFlow makes assumptions regarding how you use Elasticsearch to store read
models.

* The host application of EventFlow is responsible for creating correct
  Elasticsearch type mapping for any indexes by creating index templates

If you want to control the index a specific read model is stored in, create
create an implementation of `IReadModelDescriptionProvider` and register it
in the [EventFlow IoC](./Customize.md).

### MSSQL
