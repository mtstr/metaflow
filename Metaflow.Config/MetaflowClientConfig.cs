namespace Metaflow
{
    public class MetaflowClientConfig
    {
        public string ClusterName { get; set; }
        public bool Local { get; set; }
        public string OrleansStorage { get; set; }
        public int GatewayPort { get; set; } = 30000;
    }
}