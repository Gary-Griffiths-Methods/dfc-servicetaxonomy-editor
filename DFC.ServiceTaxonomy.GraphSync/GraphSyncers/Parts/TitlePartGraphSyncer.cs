using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using Neo4j.Driver;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Title.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts
{
    public class TitlePartGraphSyncer : IContentPartGraphSyncer
    {
        public string? PartName => nameof(TitlePart);

        public Task<IEnumerable<ICommand>> AddSyncComponents(
            dynamic graphLookupContent,
            IMergeNodeCommand mergeNodeCommand,
            IReplaceRelationshipsCommand replaceRelationshipsCommand,
            ContentTypePartDefinition contentTypePartDefinition,
            IGraphSyncHelper graphSyncHelper)
        {
            //todo: configurable??
            mergeNodeCommand.Properties.Add("skos__prefLabel", graphLookupContent.Title.ToString());

            return Task.FromResult(Enumerable.Empty<ICommand>());
        }

        public Task<bool> VerifySyncComponent(ContentItem contentItem, ContentTypePartDefinition contentTypePartDefinition, INode sourceNode,
            IEnumerable<IRelationship> relationships, IEnumerable<INode> destNodes, IGraphSyncHelper graphSyncHelper)
        {
            var prefLabel = sourceNode.Properties["skos__prefLabel"];
            return Task.FromResult(Convert.ToString(prefLabel) == Convert.ToString(contentItem.Content.TitlePart.Title));
        }
    }
}
