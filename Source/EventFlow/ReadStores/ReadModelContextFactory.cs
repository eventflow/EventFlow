using EventFlow.Configuration;

namespace EventFlow.ReadStores
{
    public class ReadModelContextFactory : IReadModelContextFactory
    {
        private readonly IResolver _resolver;

        public ReadModelContextFactory(IResolver resolver)
        {
            _resolver = resolver;
        }

        public IReadModelContext Create(string readModelId, bool isNew)
        {
            return new ReadModelContext(_resolver, readModelId, isNew);
        }
    }
}