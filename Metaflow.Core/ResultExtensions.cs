namespace Metaflow
{
    public static class ResultExtensions
    {

        public static Reject AsReject(this string reason) => new Reject(reason);

        public static Result<TResource> Replaced<TResource>(this TResource e, Maybe<TResource> before) where TResource : struct
        {
            return before.Match(Result<TResource>.Created(e), b => Result<TResource>.Replaced(b, e));
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
