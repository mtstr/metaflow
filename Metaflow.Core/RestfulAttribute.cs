using System;

namespace Metaflow
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class RestfulAttribute : Attribute
    {
    }
}
