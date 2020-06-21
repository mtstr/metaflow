using Newtonsoft.Json;

namespace Metaflow.Orleans
{
    public class Event
    {
        public Event(string entityId, bool success, int eTag, object payload)
        {
            EntityId = entityId;
            Success = success;
            ETag = eTag;
            Payload = payload;
        }

        public string EntityId { get; }

        public bool Success { get; }

        public string Id => ETag.ToString();

        [JsonProperty("etag")]
        public int ETag { get; }
        public object Payload { get; }

    }
}
