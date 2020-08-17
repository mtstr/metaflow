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
        public RestfulAttribute()
        {
        }

        public RestfulAttribute(Type deltaType)
        {
            DeltaType = deltaType;
        }

        public bool AllowImplicitCreate { get; set; }
        public Type DeltaType { get; }


    }
}
