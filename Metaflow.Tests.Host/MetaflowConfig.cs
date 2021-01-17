namespace Metaflow.Tests.Host
{
    public class MetaflowConfig
    {
        public EventStoreConfig EventStore { get; set; } = new();
        public OrleansConfig Orleans { get; set; } = new();
    }
}