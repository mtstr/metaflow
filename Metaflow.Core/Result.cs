using System.Collections.Generic;
using System.Linq;

namespace Metaflow
{
    public readonly struct Result
    {
        public bool OK { get; }
        public IReadOnlyCollection<object> Events { get; }

        public string Error { get; }

        public static Result Ok(IEnumerable<object> events) => new Result(true, events);
        public static Result Nok(string reason) => new Result(reason);
        public static Result Nok(IEnumerable<object> events, string message = "") => new Result(false, events, message);

        public static implicit operator Result(Reject reject) => Nok(reject.Reason);

        private Result(bool ok, IEnumerable<object> events, string message = "")
        {
            OK = ok;
            Events = (events?.ToList() ?? new List<object>()).AsReadOnly();
            Error = message;
        }
        private Result(string error)
        {
            OK = false;
            Error = error;
            Events = new List<object>().AsReadOnly();
        }
    }
}
