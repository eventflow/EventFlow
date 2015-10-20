# Elasticsearch read model store

Configuring EventFlow to use Elasticsearch as a store for read models is done
in steps.

1. Configure Elasticsearch connection in EventFlow
1. Configure your Elasticsearch read models in EventFlow

Given you have defined a read model class named `MyElasticsearchReadModel`, the
above will look like this.

```csharp
var resolver = EventFlowOptions.New
  .ConfigureElasticsearch(new Uri("http://localhost:9200"))
  .UseElasticsearchReadModel<MyElasticsearchReadModel>()
  ...
  .CreateResolver();
```

EventFlow makes a few assumptions regarding how you use Elasticsearch to store
read models.

* The host application of EventFlow is responsible for creating correct
  Elasticsearch type mapping for any indexes by creating index templates

If you want to control the index a specific read model is stored in, create
create an implementation of `IReadModelDescriptionProvider` and register it
in the [EventFlow IoC](./Customize.md).
