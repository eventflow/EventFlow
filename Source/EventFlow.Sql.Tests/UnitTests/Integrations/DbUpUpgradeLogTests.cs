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
using EventFlow.Logs;
using EventFlow.Sql.Integrations;
using EventFlow.TestHelpers;
using Moq;
using NUnit.Framework;

namespace EventFlow.Sql.Tests.UnitTests.Integrations
{
    [Category(Categories.Unit)]
    public class DbUpUpgradeLogTests
    {
        private Mock<ILog> _log;
        private DbUpUpgradeLog _sut;

        [SetUp]
        public void SetUp()
        {
            _log = new Mock<ILog>();
            _sut = new DbUpUpgradeLog(_log.Object);
        }

#if NET8_0_OR_GREATER
        // dbup-core 6.x IUpgradeLog surface

        [Test]
        public void LogTraceIsMappedToVerbose()
        {
            _sut.LogTrace("format {0}", 42);

            _log.Verify(
                l => l.Verbose("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void LogDebugIsMappedToDebug()
        {
            _sut.LogDebug("format {0}", 42);

            _log.Verify(
                l => l.Debug("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void LogInformationIsMappedToInformation()
        {
            _sut.LogInformation("format {0}", 42);

            _log.Verify(
                l => l.Information("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void LogWarningIsMappedToWarning()
        {
            _sut.LogWarning("format {0}", 42);

            _log.Verify(
                l => l.Warning("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void LogErrorIsMappedToError()
        {
            _sut.LogError("format {0}", 42);

            _log.Verify(
                l => l.Error("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void LogErrorWithExceptionIsMappedToError()
        {
            var exception = new InvalidOperationException();

            _sut.LogError(exception, "format {0}", 42);

            _log.Verify(
                l => l.Error(exception, "format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }
#else
        // dbup-core 4.x/5.x IUpgradeLog surface

        [Test]
        public void WriteInformationIsMappedToInformation()
        {
            _sut.WriteInformation("format {0}", 42);

            _log.Verify(
                l => l.Information("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void WriteWarningIsMappedToWarning()
        {
            _sut.WriteWarning("format {0}", 42);

            _log.Verify(
                l => l.Warning("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }

        [Test]
        public void WriteErrorIsMappedToError()
        {
            _sut.WriteError("format {0}", 42);

            _log.Verify(
                l => l.Error("format {0}", It.Is<object[]>(a => a.Length == 1 && (int) a[0] == 42)),
                Times.Once);
        }
#endif
    }
}
