.. _setup-mssql:

Microsoft SQL Server
====================

To setup EventFlow Microsoft SQL Server integration, install the NuGet
package ``EventFlow.MsSql`` and add this to your EventFlow setup.

.. code-block:: c#

    IRootResolver rootResolver = EventFlowOptions.New
      .ConfigureMsSql(MsSqlConfiguration.New
        .SetConnectionString(@"Server=.\SQLEXPRESS;Database=MyApp;User Id=sa;Password=???"))
      ...
      .CreateResolver();

After setting up Microsoft SQL Server support in EventFlow, you can
continue to configure it.

- :ref:`Event store <eventstore-mssql>`
- :ref:`Read model store <read-store-mssql>`
