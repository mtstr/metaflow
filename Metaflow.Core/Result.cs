using System.Collections.Generic;
using System.Linq;

namespace Metaflow
{
    public readonly struct Result
    {
        public bool OK { get; }
        public IReadOnlyCollection<object> Events { get; }

        public string Error { get; }

        public static Result Ok(IEnumerable<object> events) => new Result(events);
        public static Result Nok(string reason) => new Result(reason);

        public static implicit operator Result(Reject reject) => Nok(reject.Reason);

        private Result(IEnumerable<object> events)
        {
            OK = true;
            Events = (events?.ToList() ?? new List<object>()).AsReadOnly();
            Error = string.Empty;
        }
        private Result(string error)
        {
            OK = false;
            Error = error;
            Events = new List<object>().AsReadOnly();
        }
    }
}
