namespace Metaflow
{
    
    public readonly struct Result<TResource> where TResource : class, new()
    {
        public bool OK { get; }

        public TResource Before { get; }
        public TResource After { get; }
        public string Reason { get; }
        public StateChange StateChange { get; }

        public static Result<TResource> Ok(StateChange change, TResource before, TResource after) => new Result<TResource>(change, before, after);

        public static Result<TResource> Nok(string reason) => new Result<TResource>(false, change: StateChange.None, reason: reason);

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
            Before = null;
            After = null;
        }
    }
}
