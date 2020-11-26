using System;
using System.Runtime.Serialization;

namespace Metaflow.Orleans
{
    [Serializable]
    internal class EventDeserializeException : Exception
    {
        public EventDeserializeException()
        {
        }

        public EventDeserializeException(string message) : base(message)
        {
        }

        public EventDeserializeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EventDeserializeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}