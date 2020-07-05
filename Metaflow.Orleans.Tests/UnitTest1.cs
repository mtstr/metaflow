using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Metaflow.Orleans.Tests
{
    public class UnitTest1
    {

        [Fact]
        public async Task Test1()
        {
            CosmosEventRepository repo = GetRepo();

            const string eid = "e_1";

            int v = (await repo.LatestEventVersion(eid)) + 1;

            Event @event = new Event(eid, false, v, new Rejected<SampleResource, SampleInput>(MutationRequest.POST, new SampleInput { Value = "Hello" }, "invalid payload"));

            await repo.WriteEvent(@event);

            var events = await repo.ReadEvents(eid, 0).ToListAsync();

            events.Should().HaveCount(v);

            events.Should().Match(e => e.All(e => e.EntityId == eid));
        }

        private static CosmosEventRepository GetRepo()
        {
            var log = Substitute.For<ILogger<CosmosEventRepository>>();

            CosmosEventContainers cosmosContainers = GetContainers();

            var repo = new CosmosEventRepository(cosmosContainers, log);
            return repo;
        }

        [Fact]
        public async Task Test2()
        {
            CosmosEventRepository repo = GetRepo();

            const string eid = "e_1";

            SampleResource sampleResource = new SampleResource { Value = "World" };

            Snapshot<SampleResource> snapshot1 = new Snapshot<SampleResource> { EntityId = eid, ETag = 1, State = new GrainState<SampleResource> { Value = sampleResource, Exists = true } };

            await repo.WriteSnapshot(snapshot1);

            var snapshot = await repo.ReadSnapshot<SampleResource>(eid, 1);

            snapshot.EntityId.Should().Be(eid);
        }

        private static CosmosEventContainers GetContainers()
        {
            var cosmosClient = new CosmosClient("endpoint", "key");

            var snapshotContainer = cosmosClient.GetContainer("main", "Snapshots");
            var streamContainer = cosmosClient.GetContainer("main", "EventStream");

            var cosmosContainers = new CosmosEventContainers(snapshotContainer, streamContainer);
            return cosmosContainers;
        }
    }

    public class SampleResource
    {
        public string Value { get; set; }
    }

    public class SampleInput
    {
        public string Value { get; set; }
    }
}
