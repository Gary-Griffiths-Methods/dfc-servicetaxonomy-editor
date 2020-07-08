﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Services.Interfaces;
using FakeItEasy;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace DFC.ServiceTaxonomy.UnitTests.UnitTestHelpers.PartGraphSyncer
{
    public class PartGraphSyncer_AddSyncComponentsTests
    {
        //todo: remove nullable?
        public JObject? Content { get; set; }
        public ContentItem ContentItem { get; set; }
        public IMergeNodeCommand MergeNodeCommand { get; set; }
        public IReplaceRelationshipsCommand ReplaceRelationshipsCommand { get; set; }
        //todo: do we need to introduce a IContentTypePartDefinition (like ContentTypePartDefinition)
        public ContentTypePartDefinition ContentTypePartDefinition { get; set; }
        public IGraphSyncHelper GraphSyncHelper { get; set; }
        public IGraphReplicaSet GraphReplicaSet { get; set; }
        public IContentManager ContentManager { get; set; }
        public IContentItemVersion ContentItemVersion { get; set; }
        public IGraphMergeContext GraphMergeContext { get; set; }

        public IContentPartGraphSyncer? ContentPartGraphSyncer { get; set; }

        public PartGraphSyncer_AddSyncComponentsTests()
        {
            Content = JObject.Parse("{}");

            MergeNodeCommand = A.Fake<IMergeNodeCommand>();
            //todo: best way to do this?
            MergeNodeCommand.Properties = new Dictionary<string, object>();

            ReplaceRelationshipsCommand = A.Fake<IReplaceRelationshipsCommand>();

            ContentTypePartDefinition = A.Fake<ContentTypePartDefinition>();

            GraphSyncHelper = A.Fake<IGraphSyncHelper>();

            GraphReplicaSet = A.Fake<IGraphReplicaSet>();

            ContentItem = A.Fake<ContentItem>();
            ContentManager = A.Fake<IContentManager>();
            ContentItemVersion = A.Fake<IContentItemVersion>();

            GraphMergeContext = A.Fake<IGraphMergeContext>();
            A.CallTo(() => GraphMergeContext.GraphSyncHelper).Returns(GraphSyncHelper);
            A.CallTo(() => GraphMergeContext.GraphReplicaSet).Returns(GraphReplicaSet);
            A.CallTo(() => GraphMergeContext.MergeNodeCommand).Returns(MergeNodeCommand);
            A.CallTo(() => GraphMergeContext.ReplaceRelationshipsCommand).Returns(ReplaceRelationshipsCommand);
            A.CallTo(() => GraphMergeContext.ContentItem).Returns(ContentItem);
            A.CallTo(() => GraphMergeContext.ContentManager).Returns(ContentManager);
            A.CallTo(() => GraphMergeContext.ContentItemVersion).Returns(ContentItemVersion);
            A.CallTo(() => GraphMergeContext.ContentTypePartDefinition).Returns(ContentTypePartDefinition);
        }

        public async Task CallAddSyncComponents()
        {
            if (ContentPartGraphSyncer == null)
                throw new InvalidOperationException("You must set ContentPartGraphSyncer to the IContentPartGraphSyncer you want to test dummy.");

            await ContentPartGraphSyncer.AddSyncComponents(Content!, GraphMergeContext);
        }
    }
}