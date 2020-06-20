using Newtonsoft.Json;

namespace Metaflow.Orleans
{
    public class Event
    {
        public string EntityId { get; set; }

        public string Id => ETag.ToString();

        [JsonProperty("etag")]
        public int ETag { get; set; }
        public object Payload { get; set; }
    }
}
