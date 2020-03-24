using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.Models;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using Neo4j.Driver;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts
{
    public class GraphSyncPartGraphSyncer : IContentPartGraphSyncer
    {
        public string? PartName => nameof(GraphSyncPart);

        public Task<IEnumerable<ICommand>> AddSyncComponents(
            dynamic graphSyncContent,
            IMergeNodeCommand mergeNodeCommand,
            IReplaceRelationshipsCommand replaceRelationshipsCommand,
            ContentTypePartDefinition contentTypePartDefinition,
            IGraphSyncHelper graphSyncHelper)
        {
            mergeNodeCommand.Properties.Add(graphSyncHelper.IdPropertyName, graphSyncHelper.GetIdPropertyValue(graphSyncContent));

            return Task.FromResult(Enumerable.Empty<ICommand>());
        }

        public Task<bool> VerifySyncComponent(ContentItem contentItem, ContentTypePartDefinition contentTypePartDefinition, INode sourceNode,
            IEnumerable<IRelationship> relationships, IEnumerable<INode> destNodes, IGraphSyncHelper graphSyncHelper)
        {
            object id = sourceNode.Properties[graphSyncHelper.IdPropertyName];
            //todo: should we convert to string?
            return Task.FromResult(Convert.ToString(id) == graphSyncHelper.GetIdPropertyValue(contentItem.Content.GraphSyncPart));
        }
    }
}
