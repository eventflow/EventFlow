// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Autofac;
using Autofac.Integration.WebApi;
using EventFlow.Configuration.Registrations;
using EventFlow.Configuration.Registrations.Resolvers;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Owin.Extensions;
using EventFlow.Test;
using Owin;

namespace EventFlow.Owin.Tests.IntegrationTests.Site
{
    public class LogProviderExceptionLogger : IExceptionLogger
    {
        private readonly ILog _log;

        public LogProviderExceptionLogger(ILog log)
        {
            _log = log;
        }

        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            _log.Error(context.Exception, "Unhandled exception!");
            return Task.FromResult<object>(null);
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterApiControllers(typeof(Startup).Assembly).InstancePerRequest();

            var resolver = EventFlowOptions.New
                .UseRegistrationFactory(new AutofacRegistrationFactory(containerBuilder))
                .AddEvents(EventFlowTest.Assembly)
                .AddOwinMetadataProviders()
                .CreateResolver(false);

            var autofacRootResolver = (AutofacRootResolver) resolver;
            var container = autofacRootResolver.Container;

            var config = new HttpConfiguration
                {
                    DependencyResolver = new AutofacWebApiDependencyResolver(container),
                    IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.LocalOnly,
                };
            config.MapHttpAttributeRoutes();
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Services.Add(typeof(IExceptionLogger), new LogProviderExceptionLogger(resolver.Resolve<ILog>()));

            appBuilder.UseAutofacMiddleware(container);
            appBuilder.UseAutofacWebApi(config);
            appBuilder.UseWebApi(config);
        }
    }
}
