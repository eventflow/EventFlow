namespace EventFlow.ReadStores.ElasticSearch
{
    public class EsConfiguration : IEsConfiguration
    {
        public static EsConfiguration New { get { return new EsConfiguration(); } }

        public string ConnectionString { get; private set; }

        private EsConfiguration() { }

        public EsConfiguration SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }
    }
}