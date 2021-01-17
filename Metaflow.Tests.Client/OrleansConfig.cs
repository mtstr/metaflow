namespace Metaflow.Tests.Client
{
    public class OrleansConfig
    {
        public string ClusterName { get; set; }
        public bool Local { get; set; }
        public string Storage { get; set; }

        public int GatewayPort { get; set; } = 30000;
        public int SiloPort { get; set; } = 11111;
    }
}