namespace EventFlow.ReadStores
{
    public interface IReadModelContextFactory
    {
        IReadModelContext Create(string readModelId, bool isNew);
    }
}
