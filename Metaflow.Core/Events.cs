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


    public class Rejected<TResource,TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }

        public Rejected(MutationRequest request, TInput input)
        {
            Request = request.ToString();
            ResourceType = typeof(TResource).Name;
            Input = input;
        }
    }

    public class Failed<TResource,TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }
        public Exception Exception { get; }

        public Failed(MutationRequest request, TInput input, Exception ex)
        {
            Request = request.ToString();
            ResourceType = typeof(TResource).Name;
            Input = input;
            Exception = ex;
        }
    }

    public class Received<TResource,TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }

        public TInput Input { get; }

        public Received(MutationRequest request, TInput input)
        {
            Request = request.ToString();
            ResourceType = typeof(TResource).Name;
            Input = input;
        }
    }
}
