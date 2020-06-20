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
            var log = Substitute.For<ILogger<CosmosEventRepository>>();

            var cosmosClient = new CosmosClient("https://devmtstr.documents.azure.com:443/", "wX0D9FJk7jfrPcTAnT5rg1w0DDNLXA58b31rNVmb36IBiuOmNer7BbdaB95mlEyGhlyNaTEqDSo3CwCcfso1Bw==");
            var repo = new CosmosEventRepository(cosmosClient, log);

            const string eid = "e_1";

            int v = (await repo.LatestVersion(eid)) + 1;

            await repo.WriteEvent(new Event { EntityId = eid, ETag = v, Payload = new Rejected<SampleResource, SampleInput>(MutationRequest.POST, new SampleInput { Value = "Hello" }) });

            var events = await repo.ReadEvents(eid, 0).ToListAsync();

            events.Should().HaveCount(v);

            events.Should().Match(e => e.All(e => e.EntityId == eid));
        }

        [Fact]
        public async Task Test2()
        {
            var log = Substitute.For<ILogger<CosmosEventRepository>>();

            var cosmosClient = new CosmosClient("https://devmtstr.documents.azure.com:443/", "wX0D9FJk7jfrPcTAnT5rg1w0DDNLXA58b31rNVmb36IBiuOmNer7BbdaB95mlEyGhlyNaTEqDSo3CwCcfso1Bw==");
            var repo = new CosmosEventRepository(cosmosClient, log);

            const string eid = "e_1";

            await repo.WriteSnapshot(new Snapshot<SampleResource> { EntityId = eid, ETag = 1, State = new SampleResource { Value = "World" } });

            var snapshot = await repo.ReadSnapshot<SampleResource>(eid, 1);

            snapshot.EntityId.Should().Be(eid);
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
