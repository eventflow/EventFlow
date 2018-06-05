// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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

using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.DI.AutoFac;
using EventFlow.Akka.Extensions;
using EventFlow.Autofac.Extensions;
using EventFlow.Autofac.Registrations;
using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using NUnit.Framework;

namespace EventFlow.Akka.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class BasicAkkaScenario : IntegrationTest
    {
        [Test]
        public async Task Basic()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            var pingId = await PublishPingCommandAsync(thingyId).ConfigureAwait(false);

            // Assert
            await Task.Delay(5000);
        }

        private ActorSystem _actorSystem;

        [TearDown]
        public void TearDown()
        {
            _actorSystem.Dispose();
        }

        protected override IEventFlowOptions Options(IEventFlowOptions eventFlowOptions)
        {
            return base.Options(eventFlowOptions.UseAutofacContainerBuilder());
        }

        protected override IScopeResolver CreateResolver(IEventFlowOptions eventFlowOptions)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka {  
                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                    log-config-on-start = on
                    actor.debug.unhandled = on
                    actor {                
                        debug {  
                              receive = on 
                              autoreceive = on
                              lifecycle = on
                              event-stream = on
                              unhandled = on
                        }
                    }
            ");

            _actorSystem = ActorSystem.Create(A<string>(), config);

            var resolver = eventFlowOptions
                .UseAkka(_actorSystem)
                .CreateResolver();

            var autofacRootResolver = (AutofacRootResolver) resolver;

            // ReSharper disable once ObjectCreationAsStatement
            new AutoFacDependencyResolver(autofacRootResolver.Container, _actorSystem);

            return resolver;
        }
    }
}
