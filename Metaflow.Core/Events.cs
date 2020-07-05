using System;

namespace Metaflow
{
    public readonly struct Updated<TResource>
    {
        public Updated(TResource before, TResource after)
        {
            Before = before;
            After = after;
        }

        public TResource Before { get; }
        public TResource After { get; }
    }

    public readonly struct Ignored<TResource, TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }

        public Ignored(MutationRequest request, TInput input)
        {
            Request = request.ToString();
            ResourceType = typeof(TResource).Name;
            Input = input;
        }
    }

    public readonly struct Deleted<TResource>
    {
        public Deleted(TResource before)
        {
            Before = before;
        }

        public TResource Before { get; }
    }

    public readonly struct Replaced<TResource>
    {

        public Replaced(TResource before, TResource after)
        {
            Before = before;
            After = after;
        }

        public TResource Before { get; }
        public TResource After { get; }
    }

    public readonly struct Created<TResource>
    {

        public Created(TResource after)
        {
            After = after;
        }

        public TResource After { get; }
    }


    public readonly struct Rejected<TResource, TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }

        public string Reason { get; }

        public Rejected(MutationRequest request, TInput input, string reason)
        {
            Request = request.ToString();
            ResourceType = typeof(TResource).Name;
            Input = input;
            Reason = reason;
        }
    }

    public readonly struct Failed<TResource, TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }
        public string Exception { get; }

        public Failed(MutationRequest request, TInput input, string exception)
        {
            Request = request.ToString();
            ResourceType = typeof(TResource).Name;
            Input = input;
            Exception = exception;
        }
    }

    public readonly struct Received<TResource, TInput>
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
