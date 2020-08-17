using System;
using System.Runtime.Serialization;

namespace Metaflow.Orleans
{
    [Serializable]
    internal class DispatchException : Exception
    {
        public DispatchException()
        {
        }

        public DispatchException(string message) : base(message)
        {
        }

        public DispatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DispatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}