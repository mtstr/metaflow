namespace Metaflow
{
    public static class ResultExtensions
    {

        public static Reject AsReject(this string reason) => new Reject(reason);

        public static Result<TResource> Replaced<TResource>(this TResource e, Maybe<TResource> before) where TResource : struct
        {
            return before.Match(Result<TResource>.Created(e), b => Result<TResource>.Replaced(b, e));
        }
        public static object AsEvent<TResource, TInput>(this Result<TResource> result, MutationRequest request, TInput input)
        {
            return result.OK ?
                                     result.Succeeded(request, input) :
                                     result.Rejected(request, input);

        }
        public static Rejected<TResource, TInput> Rejected<TResource, TInput>(this Result<TResource> result, MutationRequest request, TInput input)
        {
            return new Rejected<TResource, TInput>(request, input, result.Reason);
        }

        public static object Succeeded<TResource, TInput>(this Result<TResource> result, MutationRequest request, TInput input)
        {
            return result.StateChange switch
            {
                StateChange.Created => new Created<TResource>(result.After),
                StateChange.Replaced => new Replaced<TResource>(result.Before, result.After),
                StateChange.Deleted => new Deleted<TResource>(result.Before),
                StateChange.Updated => new Updated<TResource>(result.Before, result.After),
                StateChange.None => new Ignored<TResource, TInput>(request, input),
                _ => throw new InvalidStateChange(request, result)
            };
        }

        public static Result<TResource> Replaced<TResource>(this TResource e, TResource before)
        {
            return Result<TResource>.Replaced(before, e);
        }

        public static Result<TResource> Created<TResource>(this TResource e)
        {
            return Result<TResource>.Created(e);
        }

        public static Result<TResource> Deleted<TResource>(this TResource d)
        {
            return Result<TResource>.Deleted(d);
        }
    }
}
