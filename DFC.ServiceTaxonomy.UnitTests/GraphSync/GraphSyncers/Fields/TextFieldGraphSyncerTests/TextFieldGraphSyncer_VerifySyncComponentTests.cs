﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Fields;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.OrchardCore.Interfaces;
using FakeItEasy;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DFC.ServiceTaxonomy.UnitTests.GraphSync.GraphSyncers.Fields.TextFieldGraphSyncerTests
{
    public class TextFieldGraphSyncer_VerifySyncComponentTests
    {
        public JObject ContentItemField { get; set; }
        public IContentPartFieldDefinition ContentPartFieldDefinition { get; set; }
        public INode SourceNode { get; set; }
        public IEnumerable<IRelationship> Relationships { get; set; }
        public IEnumerable<INode> DestinationNodes { get; set; }
        public IGraphSyncHelper GraphSyncHelper { get; set; }
        public IGraphValidationHelper GraphValidationHelper { get; set; }
        public TextFieldGraphSyncer TextFieldGraphSyncer { get; set; }

        const string _contentKey = "Text";
        //todo: test using this?
        const string _fieldName = "TestTextFieldName";

        public TextFieldGraphSyncer_VerifySyncComponentTests()
        {
            ContentItemField = JObject.Parse("{}");

            ContentPartFieldDefinition = A.Fake<IContentPartFieldDefinition>();
            A.CallTo(() => ContentPartFieldDefinition.Name).Returns(_fieldName);

            SourceNode = A.Fake<INode>();

            Relationships = new IRelationship[0];
            DestinationNodes = new INode[0];

            GraphSyncHelper = A.Fake<IGraphSyncHelper>();
            A.CallTo(() => GraphSyncHelper.PropertyName(_fieldName)).Returns(_fieldName);

            GraphValidationHelper = A.Fake<IGraphValidationHelper>();

            TextFieldGraphSyncer = new TextFieldGraphSyncer();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task VerifySyncComponentTests(bool expected, bool stringContentPropertyMatchesNodePropertyReturns)
        {
            A.CallTo(() => GraphValidationHelper.StringContentPropertyMatchesNodeProperty(
                _contentKey,
                A<JObject>._,
                _fieldName,
                SourceNode)).Returns(stringContentPropertyMatchesNodePropertyReturns);

            bool verified = await CallVerifySyncComponent();

            Assert.Equal(expected, verified);
        }

        private async Task<bool> CallVerifySyncComponent()
        {
            return await TextFieldGraphSyncer.VerifySyncComponent(
                ContentItemField,
                ContentPartFieldDefinition,
                SourceNode,
                Relationships,
                DestinationNodes,
                GraphSyncHelper,
                GraphValidationHelper);
        }
    }
}
