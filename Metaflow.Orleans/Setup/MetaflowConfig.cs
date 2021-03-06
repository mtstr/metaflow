﻿using System.Collections.Generic;

namespace Metaflow.Orleans
{
    public class MetaflowConfig
    {
        public ICollection<string> Assemblies { get; set; } = new List<string>();
        public string ClusterName { get; set; }
        public bool Local { get; set; }
        public string AzureStorage { get; set; }
        public EventStoreConfig EventStore { get; set; }

        public int GatewayPort { get; set; } = 30000;
        public int SiloPort { get; set; } = 11111;
    }
}