using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Extensions
{
    class SpecificDbContextProvider<TTarget, TProvider> : IDbContextProvider<TTarget>
        where TProvider : IDbContextProvider
    {
        private readonly TProvider _innerProvider;

        public SpecificDbContextProvider(TProvider innerProvider)
        {
            _innerProvider = innerProvider;
        }

        public DbContext CreateContext()
        {
            return _innerProvider.CreateContext();
        }
    }
}