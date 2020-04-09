﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts;
using FakeItEasy;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement.Metadata.Models;
using Xunit;

namespace DFC.ServiceTaxonomy.UnitTests.GraphSync.GraphSyncers.Parts.TitlePartGraphSyncerTests
{
    public class TitlePartGraphSyncer_VerifySyncComponentTests
    {
        public JObject Content { get; set; }
        public ContentTypePartDefinition ContentTypePartDefinition { get; set; }
        public INode SourceNode { get; set; }
        public IEnumerable<IRelationship> Relationships { get; set; }
        public IEnumerable<INode> DestinationNodes { get; set; }
        public IGraphSyncHelper GraphSyncHelper { get; set; }
        public IGraphValidationHelper GraphValidationHelper { get; set; }
        public TitlePartGraphSyncer TitlePartGraphSyncer { get; set; }

        const string _contentKey = "Title";
        const string _nodeTitlePropertyName = "skos__prefLabel";

        public TitlePartGraphSyncer_VerifySyncComponentTests()
        {
            Content = JObject.Parse("{}");

            ContentTypePartDefinition = A.Fake<ContentTypePartDefinition>();

            SourceNode = A.Fake<INode>();

            Relationships = new IRelationship[0];
            DestinationNodes = new INode[0];

            GraphSyncHelper = A.Fake<IGraphSyncHelper>();

            GraphValidationHelper = A.Fake<IGraphValidationHelper>();

            TitlePartGraphSyncer = new TitlePartGraphSyncer();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task VerifySyncComponentTests(bool expected, bool stringContentPropertyMatchesNodePropertyReturns)
        {
            A.CallTo(() => GraphValidationHelper.StringContentPropertyMatchesNodeProperty(
                _contentKey,
                A<JObject>._,
                _nodeTitlePropertyName,
                SourceNode)).Returns(stringContentPropertyMatchesNodePropertyReturns);

            bool verified = await CallVerifySyncComponent();

            Assert.Equal(expected, verified);
        }

        private async Task<bool> CallVerifySyncComponent()
        {
            return await TitlePartGraphSyncer.VerifySyncComponent(
                Content,
                ContentTypePartDefinition,
                SourceNode,
                Relationships,
                DestinationNodes,
                GraphSyncHelper,
                GraphValidationHelper);
        }
    }
}