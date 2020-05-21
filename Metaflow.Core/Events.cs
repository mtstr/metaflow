using System;

namespace Metaflow
{
    public class Updated<TResource> 
    {

        public Updated(TResource before, TResource after)
        {
            Before = before;
            After = after;
        }

        public TResource Before { get; }
        public TResource After { get; }
    }

    public class Deleted<TResource> 
    {
        public Deleted(TResource before)
        {
            Before = before;
        }

        public TResource Before { get; }
    }

    public class Replaced<TResource> 
    {

        public Replaced(TResource before, TResource after)
        {
            Before = before;
            After = after;
        }

        public TResource Before { get; }
        public TResource After { get; }
    }

    public class Created<TResource> 
    {

        public Created(TResource after)
        {
            After = after;
        }

        public TResource After { get; }
    }


    public class Rejected<TResource>
    {
        public string Request { get; }
        public TResource Resource { get; }

        public Rejected(MutationRequest request, TResource resource)
        {
            Request = request.ToString();
            Resource = resource;
        }
    }

    public class Failed<TResource>
    {
        public string Request { get; }
        public TResource Resource { get; }
        public Exception Exception { get; }

        public Failed(MutationRequest request, TResource resource, Exception ex)
        {
            Request = request.ToString();
            Resource = resource;
            Exception = ex;
        }
    }

    public class Received<TResource>
    {
        public string Request { get; }
        public TResource Resource { get; }

        public Received(MutationRequest request, TResource resource)
        {
            Request = request.ToString();
            Resource = resource;
        }
    }
}
