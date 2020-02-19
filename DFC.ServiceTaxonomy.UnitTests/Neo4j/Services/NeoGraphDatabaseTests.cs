using DFC.ServiceTaxonomy.Neo4j.Configuration;
using DFC.ServiceTaxonomy.Neo4j.Services;
using FakeItEasy;
using Microsoft.Extensions.Options;
using Xunit;

namespace DFC.ServiceTaxonomy.UnitTests.Neo4j.Services
{
    public class NeoGraphDatabaseTests
    {
        //todo: autofixture?? AutoFixture, AutoFixture.xUnit2, AutoFakeItEasy. auto on ctor possible?

        private NeoGraphDatabase _neoGraphDatabase;

        public NeoGraphDatabaseTests()
        {
            var optionsMonitor = A.Fake<IOptionsMonitor<Neo4jConfiguration>>();
            var neo4jConfiguration = A.Fake<Neo4jConfiguration>();
            neo4jConfiguration.Endpoint.Uri = "bolt://example.com";
            A.CallTo(() => optionsMonitor.CurrentValue).Returns(neo4jConfiguration);

            _neoGraphDatabase = new NeoGraphDatabase(optionsMonitor);
        }

        [Fact]
        public void NothingTest()
        {
        }
    }
}