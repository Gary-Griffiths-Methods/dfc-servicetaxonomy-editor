using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using Neo4j.Driver;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Title.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts
{
    public class TitlePartGraphSyncer : IContentPartGraphSyncer
    {
        public string? PartName => nameof(TitlePart);

        //todo: configurable??
        private const string _nodeTitlePropertyName = "skos__prefLabel";

        public Task<IEnumerable<ICommand>> AddSyncComponents(
            dynamic content,
            IMergeNodeCommand mergeNodeCommand,
            IReplaceRelationshipsCommand replaceRelationshipsCommand,
            ContentTypePartDefinition contentTypePartDefinition,
            IGraphSyncHelper graphSyncHelper)
        {
            // todo: check for Type.Null? see what happens when no title given
            mergeNodeCommand.Properties.Add(_nodeTitlePropertyName, content.Title.ToString());

            return Task.FromResult(Enumerable.Empty<ICommand>());
        }

        public Task<bool> VerifySyncComponent(
            dynamic content,
            ContentTypePartDefinition contentTypePartDefinition,
            INode sourceNode,
            IEnumerable<IRelationship> relationships,
            IEnumerable<INode> destNodes,
            IGraphSyncHelper graphSyncHelper)
        {
            object prefLabel = sourceNode.Properties[_nodeTitlePropertyName];
            return Task.FromResult(Convert.ToString(prefLabel) == Convert.ToString(content.Title));
        }
    }
}
