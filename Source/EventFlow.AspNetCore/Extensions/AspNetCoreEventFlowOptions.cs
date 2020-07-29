// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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

using System;
using EventFlow.AspNetCore.Configuration;
using EventFlow.AspNetCore.Logging;
using EventFlow.AspNetCore.MetadataProviders;
using EventFlow.AspNetCore.ServiceProvider;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EventFlow.AspNetCore.Extensions
{
    public class AspNetCoreEventFlowOptions
    {
        private readonly IEventFlowOptions _options;

        public AspNetCoreEventFlowOptions(IEventFlowOptions options)
        {
            _options = options;
        }

        public AspNetCoreEventFlowOptions AddUriMetadata()
        {
            return RegisterMetadataProvider<AddUriMetadataProvider>();
        }

        public AspNetCoreEventFlowOptions AddRequestHeadersMetadata()
        {
            return RegisterMetadataProvider<AddRequestHeadersMetadataProvider>();
        }

        public AspNetCoreEventFlowOptions AddUserHostAddressMetadata()
        {
            return RegisterMetadataProvider<AddUserHostAddressMetadataProvider>();
        }

        public AspNetCoreEventFlowOptions AddUserClaimsMetadata(params string[] includedClaimTypes)
        {
            var options = new DefaultUserClaimsMetadataOptions(includedClaimTypes);
            _options.RegisterServices(s => s.Register<IUserClaimsMetadataOptions>(_ => options));
            return RegisterMetadataProvider<AddUserClaimsMetadataProvider>();
        }

        public AspNetCoreEventFlowOptions UseLogging()
        {
            _options.RegisterServices(s => s.Register<ILog, AspNetCoreLoggerLog>());
            return this;
        }

        public AspNetCoreEventFlowOptions UseDefaults()
        {
            return RunBootstrapperOnHostStartup().AddDefaultMetadataProviders();
        }

        public AspNetCoreEventFlowOptions RunBootstrapperOnHostStartup()
        {
            _options.RegisterServices(s => s.Register<IHostedService, HostedBootstrapper>(Lifetime.Singleton));
            return this;
        }

        public AspNetCoreEventFlowOptions AddDefaultMetadataProviders()
        {
            AddRequestHeadersMetadata();
            AddUriMetadata();
            AddUserHostAddressMetadata();
            return this;
        }

#if NETSTANDARD2_0
        public AspNetCoreEventFlowOptions UseMvcJsonOptions()
        {
            _options.RegisterServices(s =>
                s.Register<IConfigureOptions<MvcJsonOptions>, EventFlowJsonOptionsMvcConfiguration>());
            return this;
        }
#endif
#if (NETCOREAPP3_0 || NETCOREAPP3_1)
        public AspNetCoreEventFlowOptions UseMvcJsonOptions()
        {
            _options.RegisterServices(s =>
                s.Register<IConfigureOptions<MvcNewtonsoftJsonOptions>, EventFlowJsonOptionsMvcConfiguration>());
            return this;
        }
#endif

        public AspNetCoreEventFlowOptions UseModelBinding(
            Action<EventFlowModelBindingMvcConfiguration> configureModelBinding = null)
        {
            var modelBindingOptions = new EventFlowModelBindingMvcConfiguration();
            configureModelBinding?.Invoke(modelBindingOptions);
            _options.RegisterServices(s => s.Register<IConfigureOptions<MvcOptions>>(c => modelBindingOptions));
            return this;
        }

        private AspNetCoreEventFlowOptions RegisterMetadataProvider<T>() where T : class, IMetadataProvider
        {
            _options
                .AddMetadataProvider<T>()
                .RegisterServices(s =>
                    s.Register<IHttpContextAccessor, HttpContextAccessor>(Lifetime.Singleton, true));

            return this;
        }
    }
}
