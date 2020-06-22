namespace Metaflow.Orleans
{
    public class CosmosDbConfig
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string Database { get; set; }
        public string EventStreamContainer { get; set; }
        public string SnapshotsContainer { get; set; }
    }
}