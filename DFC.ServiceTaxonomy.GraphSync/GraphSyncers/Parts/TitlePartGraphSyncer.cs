using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Title.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts
{
    public class TitlePartGraphSyncer : IContentPartGraphSyncer
    {
        public string? PartName => nameof(TitlePart);

        //todo: configurable??
        private const string _nodeTitlePropertyName = "skos__prefLabel";

        public Task AddSyncComponents(
            dynamic content,
            IMergeNodeCommand mergeNodeCommand,
            IReplaceRelationshipsCommand replaceRelationshipsCommand,
            ContentTypePartDefinition contentTypePartDefinition,
            IGraphSyncHelper graphSyncHelper)
        {
            JValue titleValue = content.Title;
            if (titleValue.Type != JTokenType.Null)
                mergeNodeCommand.Properties.Add(_nodeTitlePropertyName, titleValue.As<string>());

            return Task.CompletedTask;
        }

        public Task<(bool verified, string failureReason)> VerifySyncComponent(dynamic content,
            ContentTypePartDefinition contentTypePartDefinition,
            INode sourceNode,
            IEnumerable<IRelationship> relationships,
            IEnumerable<INode> destinationNodes,
            IGraphSyncHelper graphSyncHelper,
            IGraphValidationHelper graphValidationHelper,
            IDictionary<string, int> expectedRelationshipCounts)
        {
            return Task.FromResult(graphValidationHelper.StringContentPropertyMatchesNodeProperty(
                "Title",
                content,
                _nodeTitlePropertyName,
                sourceNode));
        }
    }
}
