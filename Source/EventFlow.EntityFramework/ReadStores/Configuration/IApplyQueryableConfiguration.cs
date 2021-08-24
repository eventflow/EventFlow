using System.Linq;
using EventFlow.ReadStores;

namespace EventFlow.EntityFramework.ReadStores.Configuration
{
    /// <summary>
    /// Configures an IQueryable
    /// </summary>
    /// <typeparam name="TReadModel">Entity type</typeparam>
    public interface IApplyQueryableConfiguration<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        /// <summary>
        /// Applies the expression to the IQueryable
        /// </summary>
        /// <param name="queryable">Source</param>
        /// <returns>The applied IQueryable</returns>
        IQueryable<TReadModel> Apply(IQueryable<TReadModel> queryable);
    }
}