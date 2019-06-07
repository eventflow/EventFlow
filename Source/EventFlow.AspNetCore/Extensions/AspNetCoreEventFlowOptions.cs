// The MIT License (MIT)
// 
// Copyright (c) 2015-2019 Rasmus Mikkelsen
// Copyright (c) 2015-2019 eBay Software Foundation
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
using EventFlow.AspNetCore.ModelBinding;
using EventFlow.AspNetCore.ServiceProvider;
using EventFlow.Configuration;
using EventFlow.Configuration.Serialization;
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
        private bool _hasHttpContextAccessor;

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
            return Register<ILog, AspNetCoreLoggerLog>();
        }

        public AspNetCoreEventFlowOptions UseDefaults()
        {
            return RunBootstrapperOnHostStartup().AddMetadataProviders();
        }

        public AspNetCoreEventFlowOptions RunBootstrapperOnHostStartup()
        {
            return Register<IHostedService, HostedBootstrapper>(Lifetime.Singleton);
        }

        public AspNetCoreEventFlowOptions AddMetadataProviders()
        {
            _options.AddMetadataProviders(typeof(AspNetCoreEventFlowOptions).Assembly);
            return this;
        }

        public AspNetCoreEventFlowOptions UseMvcJsonOptions()
        {
            _options.RegisterServices(s => s.Register<IConfigureOptions<MvcJsonOptions>, EventFlowJsonOptionsMvcConfiguration>());
            return this;
        }

        public AspNetCoreEventFlowOptions UseModelBinding(Action<EventFlowModelBindingMvcConfiguration> configureModelBinding = null)
        {
            var modelBindingOptions = new EventFlowModelBindingMvcConfiguration();
            configureModelBinding?.Invoke(modelBindingOptions);
            _options.RegisterServices(s => s.Register<IConfigureOptions<MvcOptions>>(c => modelBindingOptions));
            return this;
        }

        private AspNetCoreEventFlowOptions RegisterMetadataProvider<T>() where T : class, IMetadataProvider
        {
            if (!_hasHttpContextAccessor)
            {
                Register<IHttpContextAccessor, HttpContextAccessor>(Lifetime.Singleton);
                _hasHttpContextAccessor = true;
            }

            _options.AddMetadataProvider<T>();
            return this;
        }

        private AspNetCoreEventFlowOptions Register<TService, T>(Lifetime lifetime = Lifetime.AlwaysUnique)
            where T : class, TService
            where TService : class
        {
            _options.RegisterServices(s => s.Register<TService, T>(lifetime));
            return this;
        }
    }
}
