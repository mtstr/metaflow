namespace Metaflow
{
    public readonly struct Reject
    {
        public readonly string Reason;
        public Reject(string reason)
        {
            Reason = reason;
        }
    }
}
