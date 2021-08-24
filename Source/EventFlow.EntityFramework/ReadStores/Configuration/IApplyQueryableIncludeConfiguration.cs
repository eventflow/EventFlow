using System.Linq;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore.Query;

namespace EventFlow.EntityFramework.ReadStores.Configuration
{
    /// <summary>
    /// Configures an IQueryable
    /// </summary>
    /// <typeparam name="TReadModel">Entity type</typeparam>
    /// <typeparam name="TProperty">Property type</typeparam>
    public interface IApplyQueryableIncludeConfiguration<TReadModel, out TProperty>
        : IApplyQueryableConfiguration<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        /// <summary>
        /// Applies the include expression to the IQueryable
        /// </summary>
        /// <param name="queryable">Source</param>
        /// <returns>An IIncludableQueryable</returns>
        new IIncludableQueryable<TReadModel, TProperty> Apply(IQueryable<TReadModel> queryable);
    }
}