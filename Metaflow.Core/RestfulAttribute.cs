using System;

namespace Metaflow
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class RestfulAttribute : Attribute
    {
        public bool AllowImplicitCreate { get; set; }
    }
}
