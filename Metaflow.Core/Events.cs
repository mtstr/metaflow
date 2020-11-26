using System;
using System.Text.Json.Serialization;

namespace Metaflow
{
    public readonly struct Updated<TResource>
    {
        [JsonConstructor]
        public Updated(TResource before, TResource after)
        {
            Before = before;
            After = after;
        }

        public TResource Before { get; }
        public TResource After { get; }
    }

    public readonly struct Ignored<TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }

        [JsonConstructor]
        public Ignored(MutationRequest request, string resource, TInput input)
        {
            Request = request.ToString();
            ResourceType = resource;
            Input = input;
        }
    }

    public readonly struct Deleted<TResource>
    {
        [JsonConstructor]
        public Deleted(TResource before)
        {
            Before = before;
        }

        public TResource Before { get; }
    }

    public readonly struct Replaced<TResource>
    {
        [JsonConstructor]
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
        [JsonConstructor]
        public Created(TResource after)
        {
            After = after;
        }

        public TResource After { get; }
    }

    public readonly struct Upgraded<TResource>
    {
        [JsonConstructor]
        public Upgraded(TResource after)
        {
            After = after;
        }

        public TResource After { get; }
    }


    public readonly struct Rejected<TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }

        public string Reason { get; }

        [JsonConstructor]
        public Rejected(MutationRequest request, string resource, TInput input, string reason)
        {
            Request = request.ToString();
            ResourceType = resource;
            Input = input;
            Reason = reason;
        }
    }

    public readonly struct Failed<TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }
        public TInput Input { get; }
        public string Exception { get; }

        [JsonConstructor]
        public Failed(MutationRequest request, string resource, TInput input, string exception)
        {
            Request = request.ToString();
            ResourceType = resource;
            Input = input;
            Exception = exception;
        }
    }

    public readonly struct Received<TInput>
    {
        public string Request { get; }
        public string ResourceType { get; }

        public TInput Input { get; }

        [JsonConstructor]
        public Received(MutationRequest request, string resourceType, TInput input)
        {
            Request = request.ToString();
            ResourceType = resourceType;
            Input = input;
        }
    }
}