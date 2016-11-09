using EventFlow.Firebase.ReadStores;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Firebase.Tests.UnitTests
{
    [Category(Categories.Unit)]
    [TestFixture]
    public class ReadModelDescriptionProviderTests : TestsFor<ReadModelDescriptionProvider>
    {
        private class TestReadModelA : IReadModel
        {
        }

        [Test]
        public void ReadModelIndexIsCorrectWithoutAttribute()
        {
            // Act
            var readModelDescription = Sut.GetReadModelDescription<TestReadModelA>();

            // Assert
            readModelDescription.NodeName.Value.Should().Be("eventflow-testreadmodela");
        }
    }
}
