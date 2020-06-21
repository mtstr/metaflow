using Newtonsoft.Json;

namespace Metaflow.Orleans
{
    public class Snapshot<T>
    {
        public string Id => ETag.ToString();
        public string EntityId { get; set; }
        
        [JsonProperty("etag")]
        public int ETag { get; set; }
        public GrainState<T> State { get; set; }
    }
}
