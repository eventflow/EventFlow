using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using EventFlow.Autofac.Extensions;
using EventFlow.Extensions;
using EventFlow.Scenarios.Mvc5.Controllers;
using EventFlow.Scenarios.Mvc5.Domain;

namespace EventFlow.Scenarios.Mvc5
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<HomeController>().InstancePerLifetimeScope();

            var container = EventFlowOptions.New
                .UseAutofacContainerBuilder(containerBuilder)
                .AddEvents(typeof(ExampleEvent))
                .AddCommands(typeof(ExampleCommand))
                .AddCommandHandlers(typeof(ExampleCommandHandler))
                .UseInMemoryReadStoreFor<ExampleReadModel>()
                .CreateContainer();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}