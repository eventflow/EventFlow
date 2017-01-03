.. _queries:

Queries
=======

Creating queries in EventFlow is simple.

First create a value object that contains the data required for the
query. In this example we want to search for users based on their
username.

.. code-block:: c#

    public class GetUserByUsernameQuery : IQuery<User>
    {
      public string Username { get; }

      public GetUserByUsernameQuery(string username)
      {
        Username = username;
      }
    }

Next create a query handler that implements how the query is processed.

.. code-block:: c#

    public class GetUserByUsernameQueryHandler :
      IQueryHandler<GetUserByUsernameQuery, User>
    {
      private IUserReadModelRepository _userReadModelRepository;

      public GetUserByUsernameQueryHandler(
        IUserReadModelRepository userReadModelRepository)
      {
        _userReadModelRepository = userReadModelRepository;
      }

      Task<User> ExecuteQueryAsync(
        GetUserByUsernameQuery query,
        CancellationToken cancellationToken)
      {
        return _userReadModelRepository.GetByUsernameAsync(
          query.Username,
          cancellationToken)
      }
    }

Last step is to register the query handler in EventFlow. Here we show
the simple, but cumbersome version, you should use one of the overloads
that scans an entire assembly.

.. code-block:: c#

    ...
    EventFlowOptions.New
      .AddQueryHandler<GetUserByUsernameQueryHandler, GetUserByUsernameQuery, User>()
    ...

Then in order to use the query in your application, you need a reference
to the ``IQueryProcessor``, which in our case is stored in the
``_queryProcessor`` field.

.. code-block:: c#

    ...
    var user = await _queryProcessor.ProcessAsync(
      new GetUserByUsernameQuery("root")
      cancellationToken)
      .ConfigureAwait(false);
    ...

Queries shipped with EventFlow
------------------------------

-  ``ReadModelByIdQuery<TReadModel>``: Supported by both the in-memory
   and MSSQL read model stores automatically as soon as you define the
   read model use using the EventFlow options for that store
-  ``InMemoryQuery<TReadModel>``: Takes a ``Predicate<TReadModel>`` and
   returns ``IEnumerable<TReadModel>``, making it possible to search all
   your in-memory read models based on any predicate
