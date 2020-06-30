﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.Queries.Models;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.EmbeddedContentItemsGraphSyncer
{
    public interface IEmbeddedContentItemsGraphSyncer
    {
        Task AddSyncComponents(JArray? contentItems,
            IContentManager contentManager,
            IReplaceRelationshipsCommand replaceRelationshipsCommand,
            IGraphSyncHelper graphSyncHelper);

        Task<(bool validated, string failureReason)> ValidateSyncComponent(JArray? contentItems,
            IContentManager contentManager,
            INodeWithOutgoingRelationships nodeWithOutgoingRelationships,
            IGraphValidationHelper graphValidationHelper,
            IDictionary<string, int> expectedRelationshipCounts,
            string endpoint);
    }
}