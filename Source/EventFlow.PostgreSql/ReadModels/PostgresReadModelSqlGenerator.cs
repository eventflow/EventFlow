using EventFlow.Sql.ReadModels;

namespace EventFlow.PostgreSql.ReadModels
{
	public class PostgresReadModelSqlGenerator : ReadModelSqlGenerator
	{
		public PostgresReadModelSqlGenerator()
		{
			QuotedIdentifierPrefix = "\"";
			QuotedIdentifierSuffix = "\"";
		}
	}
}