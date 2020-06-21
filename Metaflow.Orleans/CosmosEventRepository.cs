using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Metaflow.Orleans
{
    public class CosmosEventRepository : IEventRepository
    {
        private readonly CosmosEventContainers _containers;
        private static readonly JsonSerializer Serializer = new JsonSerializer()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        private readonly ILogger<CosmosEventRepository> _log;

        public CosmosEventRepository(CosmosEventContainers containers, ILogger<CosmosEventRepository> log)
        {
            _containers = containers;
            _log = log;
        }
        public Task<int> LatestEventVersion(string entityId, int? etag = null)
        {
            return LatestVersion(entityId, EventStream(), etag);
        }

        public Task<int> LatestSnapshotVersion(string entityId, int? etag = null)
        {
            return LatestVersion(entityId, Snapshots(), etag);
        }

        private async Task<int> LatestVersion(string entityId, Container container, int? etag = null)
        {
            QueryDefinition query = etag.HasValue ?
             new QueryDefinition($"select VALUE MAX(s.etag) from {container.Id} s where s.etag <= @etagParam").WithParameter("@etagParam", etag) :
             new QueryDefinition($"select VALUE MAX(s.etag) from {container.Id} s");

            using FeedIterator<int> streamResultSet = container.GetItemQueryIterator<int>(
                query,
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey(entityId)
                });

            var v = (await streamResultSet.ReadNextAsync()).Resource.FirstOrDefault();

            return v;
        }

        public async IAsyncEnumerable<Event> ReadEvents(string entityId, int startingEtag, int? targetEtag = null)
        {
            QueryDefinition query = targetEtag.HasValue ?
             new QueryDefinition("select * from EventStream s where s.etag > @etagParam AND s.etag<=@targetEtagParam ORDER BY s.etag ASC")
             .WithParameter("@etagParam", startingEtag)
             .WithParameter("@targetEtagParam", targetEtag) :
            new QueryDefinition("select * from EventStream s where s.etag > @etagParam ORDER BY s.etag ASC").WithParameter("@etagParam", startingEtag);

            using (FeedIterator streamResultSet = EventStream().GetItemQueryStreamIterator(
                query,
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey(entityId)
                }))
            {
                while (streamResultSet.HasMoreResults)
                {
                    using ResponseMessage responseMessage = await streamResultSet.ReadNextAsync();

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        EventCollection collection = FromStream<EventCollection>(responseMessage.Content);

                        foreach (var e in collection.Documents)
                            yield return e;
                    }
                    else
                    {
                        _log.LogError(50013, $"Reading events failed. Status code: {responseMessage.StatusCode} Message: {responseMessage.ErrorMessage}");
                    }
                }
            }

        }

        public async Task<Snapshot<T>> ReadSnapshot<T>(string entityId, int etag)
        {
            ItemResponse<Snapshot<T>> response = await Snapshots().ReadItemAsync<Snapshot<T>>(
                partitionKey: new PartitionKey(entityId),
                id: etag.ToString());

            return response;
        }

        public async Task WriteEvent(Event @event)
        {
            using (Stream stream = ToStream(@event))
            {
                using (ResponseMessage responseMessage = await EventStream().UpsertItemStreamAsync(
                    partitionKey: new PartitionKey(@event.EntityId),
                    streamPayload: stream))
                {
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        _log.LogError(50011, $"Event write failed. Status code: {responseMessage.StatusCode} Message: {responseMessage.ErrorMessage}");
                    }
                }
            }
        }

        public async Task WriteSnapshot<T>(Snapshot<T> snapshot)
        {
            using (Stream stream = ToStream(snapshot))
            {
                using (ResponseMessage responseMessage = await Snapshots().UpsertItemStreamAsync(
                    partitionKey: new PartitionKey(snapshot.EntityId),
                    streamPayload: stream))
                {
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        _log.LogError(50012, $"Snapshot write failed. Status code: {responseMessage.StatusCode} Message: {responseMessage.ErrorMessage}");
                    }
                }
            }
        }

        private Container EventStream()
        {
            return _containers.EventStream;
        }

        private Container Snapshots()
        {
            return _containers.Snapshots;
        }

        private static T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                using StreamReader sr = new StreamReader(stream);
                using JsonTextReader jsonTextReader = new JsonTextReader(sr);
                return Serializer.Deserialize<T>(jsonTextReader);
            }
        }

        private static Stream ToStream(object input)
        {
            MemoryStream streamPayload = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(streamPayload, encoding: Encoding.Default, bufferSize: 1024, leaveOpen: true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    Serializer.Serialize(writer, input);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            // streamPayload.Position = 0;

            // using var reader = new StreamReader(streamPayload);
            // var s = reader.ReadToEnd();

            streamPayload.Position = 0;
            return streamPayload;
        }
    }

    public class EventCollection
    {
        public IEnumerable<Event> Documents { get; set; }
    }
}
