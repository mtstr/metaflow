using System;
using System.Runtime.Serialization;

namespace Metaflow
{
    [Serializable]
    public class InvalidStateChange : Exception
    {


        public InvalidStateChange()
        {
        }

        public InvalidStateChange(string message) : base(message)
        {
        }

        public InvalidStateChange(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InvalidStateChange(MutationRequest request, object result)
        {
            Request = request.ToString();
            Result = result;
        }

        protected InvalidStateChange(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public string Request { get; }
        public object Result { get; }
    }
}
