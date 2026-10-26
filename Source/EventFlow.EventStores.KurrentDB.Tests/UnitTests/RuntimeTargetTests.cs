// The MIT License (MIT)
//
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using KurrentDB.Client;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.EventStores.KurrentDB.Tests.UnitTests
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class RuntimeTargetTests
    {
        [Test]
        public void LoadsAdapterAndClientAssetsForTheRequestedRuntime()
        {
#if NET48
            const string expectedFramework = ".NETFramework,Version=v4.8";
            Assert.That(RuntimeInformation.FrameworkDescription, Does.StartWith(".NET Framework"));
            var settings = KurrentDBClientSettings.Create(TestConnectionStrings.Persistence);
            using (var handler = settings.CreateHttpMessageHandler())
            {
                Assert.That(handler.GetType().FullName, Is.EqualTo("System.Net.Http.WinHttpHandler"));
            }
#else
            const string expectedFramework = ".NETCoreApp,Version=v8.0";
            Assert.That(Environment.Version.Major, Is.GreaterThanOrEqualTo(8));
#endif
            Assert.That(typeof(KurrentDBEventPersistence).Assembly
                .GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName, Is.EqualTo(expectedFramework));
            Assert.That(typeof(KurrentDBClient).Assembly
                .GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName, Is.EqualTo(expectedFramework));
            TestContext.WriteLine(RuntimeInformation.FrameworkDescription);
            TestContext.WriteLine(Environment.OSVersion.VersionString);
        }
    }
}
