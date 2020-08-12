using System;

namespace Metaflow
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class RestfulAttribute : Attribute
    {
        public RestfulAttribute()
        {
        }

        public RestfulAttribute(Type deltaType){
            DeltaType = deltaType;
        }

        public bool AllowImplicitCreate { get; set; }
        public Type DeltaType { get; }

        
    }
}
