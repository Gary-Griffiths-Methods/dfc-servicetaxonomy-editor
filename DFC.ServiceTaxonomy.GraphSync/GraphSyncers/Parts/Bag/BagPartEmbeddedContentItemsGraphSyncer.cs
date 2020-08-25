﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Helpers;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Contexts;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.EmbeddedContentItemsGraphSyncer;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Helpers;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Flows.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts.Bag
{
    public class BagPartEmbeddedContentItemsGraphSyncer : EmbeddedContentItemsGraphSyncer, IBagPartEmbeddedContentItemsGraphSyncer
    {
        public BagPartEmbeddedContentItemsGraphSyncer(
            IContentDefinitionManager contentDefinitionManager,
            IServiceProvider serviceProvider,
            ILogger<BagPartEmbeddedContentItemsGraphSyncer> logger)
            : base(contentDefinitionManager, serviceProvider, logger)
        {
        }

        protected override IEnumerable<string> GetEmbeddableContentTypes(IGraphSyncContext context)
        {
            BagPartSettings bagPartSettings = context.ContentTypePartDefinition.GetSettings<BagPartSettings>();
            return bagPartSettings.ContainedContentTypes;
        }

        protected override async Task<string> RelationshipType(ISyncNameProvider embeddedContentSyncNameProvider)
        {
            //todo: what if want different relationships for same contenttype in different bags!
            string? relationshipType = embeddedContentSyncNameProvider.GraphSyncPartSettings.BagPartContentItemRelationshipType;
            if (string.IsNullOrEmpty(relationshipType))
                relationshipType = await base.RelationshipType(embeddedContentSyncNameProvider);

            return relationshipType;
        }
    }
}
