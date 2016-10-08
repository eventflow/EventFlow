.. _setup-mssql:

Microsoft SQL Server
====================

To setup EventFlow Microsoft SQL Server integration, install the NuGet
package ``EventFlow.MsSql`` and add this to your EventFlow setup.

Configuration
-------------

Configure the MSSQL connection as shown here.

.. code-block:: c#

    IRootResolver rootResolver = EventFlowOptions.New
      .ConfigureMsSql(MsSqlConfiguration.New
        .SetConnectionString(@"Server=.\SQLEXPRESS;Database=MyApp;User Id=sa;Password=???"))
      ...
      .CreateResolver();
