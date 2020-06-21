using Microsoft.Azure.Cosmos;

namespace Metaflow.Orleans
{
    public class CosmosEventContainers
    {
        public Container Snapshots { get; }
        public Container EventStream { get; }


        public CosmosEventContainers(Container snapshots, Container eventStream)
        {
            Snapshots = snapshots;
            EventStream = eventStream;
        }

    }
}
