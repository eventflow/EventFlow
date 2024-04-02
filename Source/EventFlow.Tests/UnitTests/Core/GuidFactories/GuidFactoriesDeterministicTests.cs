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
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core.GuidFactories
{
    [Category(Categories.Unit)]
    public class GuidFactoriesDeterministicTests
    {
        [Test]
        public void Create_EmptyNamespaceId_ThrowsArgumentNullException()
        {
            var fixture = new Fixture();
            Assert.Throws<ArgumentNullException>(() =>
                EventFlow.Core.GuidFactories.Deterministic.Create(default, fixture.CreateMany<byte>().ToArray()));
        }

        [Test]
        public void Create_EmptyNameBytes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                EventFlow.Core.GuidFactories.Deterministic.Create(Guid.NewGuid(), new byte[0]));
        }
        
        [TestCaseSource(nameof(GetTestCases))]
        public void Create(Guid namespaceId, byte[] nameBytes, Guid expected)
        {
            var result = EventFlow.Core.GuidFactories.Deterministic.Create(namespaceId, nameBytes);
            result.Should().Be(expected);
        }

        private static IEnumerable<TestCaseData> GetTestCases()
        {
            yield return new TestCaseData(new Guid("32154d4f-5b8b-4f9d-a757-af34d371cc95"), new byte[] {0}, new Guid("92f44232-16c4-541d-91b4-58fc358a6154"));
            yield return new TestCaseData(new Guid("273ddb19-d0c6-42fb-b48a-0f697154a4a3"), new byte[] {255}, new Guid("be40809c-3604-5263-bbb4-132a626d9d2a"));
            yield return new TestCaseData(new Guid("0e480ccc-7920-484d-a41f-c124d4bc6e57"), new byte[] {168, 0}, new Guid("23d0bb97-a0c8-5732-833e-258b17f56020"));
            yield return new TestCaseData(new Guid("63b52520-c1af-47fd-8ac7-fd767cdbb87c"), new byte[] {82, 4, 84}, new Guid("fd3e8003-2006-5015-abd4-d83c24555d9f"));
            yield return new TestCaseData(new Guid("8fa98f67-7ee8-4391-b09e-067d134dee21"), new byte[] {9, 83, 7, 3}, new Guid("e60c61bb-b945-5842-88de-036e80e17944"));
            yield return new TestCaseData(new Guid("73a2f9f7-8ab3-4c82-97c4-e32f0261cdd6"), new byte[] {5, 115, 57, 38, 4}, new Guid("f61e9184-2be1-5509-9735-856076c61934"));
            yield return new TestCaseData(new Guid("f2ac67d1-dc7c-4408-bab7-3826b77f82c9"), new byte[] {71, 72, 66, 66, 0, 58}, new Guid("a65682c5-da7c-510e-b59c-a96b27cb19a3"));
            yield return new TestCaseData(new Guid("a65f7892-2290-4a67-ad10-819055127fff"), new byte[] {148, 99, 29, 52, 73, 153, 6}, new Guid("0dd2fb0c-5b2d-57d0-a4f9-403093a8ccd3"));
            yield return new TestCaseData(new Guid("513cee44-7e62-4346-8fc5-56b02eaebebd"), new byte[] {85, 29, 97, 86, 93, 26, 83, 1}, new Guid("886a2596-a6d9-570a-91cc-c0a5eb4cbae8"));
            yield return new TestCaseData(new Guid("d168fede-b51d-4c6e-86ca-37402bd38ace"), new byte[] {2, 71, 53, 64, 57, 6, 58, 84, 3}, new Guid("36c800c7-574b-5633-81c6-0f882a3fa265"));
            yield return new TestCaseData(new Guid("3c34b3d5-2644-4080-8508-14d4be6083e5"), new byte[] {0, 0, 5, 96, 95, 50, 8, 171, 90, 24, 0, 59, 47, 5, 127, 96, 4, 197, 2}, new Guid("71995408-b187-5d71-b819-ef85926141c4"));
        }
    }
}
