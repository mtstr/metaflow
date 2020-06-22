namespace Metaflow
{


    public readonly struct Result<TResource>
    {
        public bool OK { get; }

        public TResource Before { get; }
        public TResource After { get; }
        public string Reason { get; }
        public StateChange StateChange { get; }

        public static Result<TResource> Created(TResource after) => new Result<TResource>(StateChange.Created, default, after);
        public static Result<TResource> Replaced(TResource before, TResource after) => new Result<TResource>(StateChange.Replaced, before, after);
        public static Result<TResource> Deleted(TResource before) => new Result<TResource>(StateChange.Deleted, before, default);
        public static Result<TResource> Updated(TResource before, TResource after) => new Result<TResource>(StateChange.Updated, before, after);

        public static Result<TResource> Nok(string reason) => new Result<TResource>(false, change: StateChange.None, reason: reason);

        public static implicit operator Result<TResource>(Reject reject) => Nok(reject.Reason);

        private Result(StateChange change, TResource before, TResource after) : this(true, change)
        {
            Before = before;
            After = after;
        }
        private Result(bool ok, StateChange change = StateChange.None, string reason = "")
        {
            OK = ok;
            Reason = reason;
            StateChange = change;
            Before = default;
            After = default;
        }
    }
}
