using System;

namespace Metaflow
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RestfulResourceAttribute : Attribute
    {
        public RestfulResourceAttribute()
        {
        }

        public RestfulResourceAttribute(Type resourceType)
        {
            ResourceType = resourceType;
        }

        public Type ResourceType { get; }
    }
    
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class RestfulAttribute : Attribute
    {
        public RestfulAttribute(int version = 1)
        {
            this.Version = version;
        }

        public int Version { get; }

        public RestfulAttribute(Type deltaType, int version = 1): this(version)
        {
            DeltaType = deltaType;
        }

        public bool AllowImplicitCreate { get; set; }
        public Type DeltaType { get; }


    }
}
