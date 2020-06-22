namespace Metaflow.Orleans
{
    public class OrleansConfig
    {
        public string ClusterName { get; set; }
        public bool Local { get; set; }
        public string AzureStorage { get; set; }
        public CosmosDbConfig CosmosDb { get; set; }
    }
}