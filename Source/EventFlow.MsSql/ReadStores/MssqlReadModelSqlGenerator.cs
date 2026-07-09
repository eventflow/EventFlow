using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using EventFlow.Sql.ReadModels;

namespace EventFlow.MsSql.ReadStores
{
    public class MssqlReadModelSqlGenerator : ReadModelSqlGenerator
    {
        private readonly IMsSqlConfiguration _configuration;
        private readonly ConcurrentDictionary<Type, string> TableNames = new ConcurrentDictionary<Type, string>();
    
        public MssqlReadModelSqlGenerator(IMsSqlConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override string GetTableName(Type readModelType)
        {
            return TableNames.GetOrAdd(
                readModelType,
                t =>
                {
                    var qip = Configuration.TableQuotedIdentifierPrefix;
                    var qis = Configuration.TableQuotedIdentifierSuffix;

                    var tableAttribute = t.GetTypeInfo().GetCustomAttribute<TableAttribute>(false);
                    var table = string.IsNullOrEmpty(tableAttribute?.Name)
                        ? $"ReadModel-{t.Name.Replace("ReadModel", string.Empty)}"
                        : tableAttribute.Name;
                    
                    var schema = string.IsNullOrEmpty(tableAttribute?.Schema)
                        ? _configuration.Schema.Value
                        : tableAttribute.Schema;
                    
                    return $"{qip}{schema}{qis}.{qip}{table}{qis}";
                });
        }
    }
}