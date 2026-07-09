using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.UnitTests
{
    [Category(Categories.Unit)]
    public class MsSqlConfigurationTests
    {
        [Test]
        public void NewUsesDboSchemaByDefault()
        {
            var configuration = MsSqlConfiguration.New;

            Assert.That(configuration.Schema.Value, Is.EqualTo("dbo"));
        }

        [Test]
        public void SetSchemaUpdatesSchemaAndReturnsSameInstance()
        {
            var configuration = MsSqlConfiguration.New;

            var updatedConfiguration = configuration.SetSchema(new Schema("eventflow"));

            Assert.That(updatedConfiguration.Schema.Value, Is.EqualTo("eventflow"));
            Assert.That(updatedConfiguration, Is.SameAs(configuration));
        }
    }
}