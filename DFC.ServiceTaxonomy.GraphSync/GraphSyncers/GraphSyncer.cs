using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.Models;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Services;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers
{
    // Type mappings
    // -------------
    // OC UI Field Type | OC Content | Neo Driver    | Cypher     | NSMNTX postfix | RDF             | Notes
    // Boolean            ?            see notes       Boolean                       xsd:boolean       neo docs say driver is boolean. do they mean Boolean or bool?
    // Content Picker
    // Date
    // Date Time
    // Html               Html         string          String
    // Link
    // Markdown
    // Media
    // Numeric            Value        long            Integer                       xsd:integer       \ OC UI has only numeric, which it presents as a real in content. do we always consistently map to a long or float, or allow user to differentiate with metadata?
    // Numeric            Value        float           Float                                           / (RDF supports xsd:int & xsd:integer, are they different or synonyms)
    // Text               Text         string          String                        xsd:string        no need to specify in RDF - default?
    // Time
    // Youtube
    //
    // see
    // https://github.com/neo4j/neo4j-dotnet-driver
    // https://www.w3.org/2011/rdf-wg/wiki/XSD_Datatypes
    // https://neo4j.com/docs/labs/nsmntx/current/import/

    public class GraphSyncer : IGraphSyncer
    {
        private readonly IGraphDatabase _graphDatabase;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentPartGraphSyncer> _partSyncers;
        private readonly IGraphSyncPartIdProperty _graphSyncPartIdProperty;
        private readonly IMergeNodeCommand _mergeNodeCommand;
        private readonly IReplaceRelationshipsCommand _replaceRelationshipsCommand;

        public GraphSyncer(
            IGraphDatabase graphDatabase,
            IContentDefinitionManager contentDefinitionManager,
            IEnumerable<IContentPartGraphSyncer> partSyncers,
            IGraphSyncPartIdProperty graphSyncPartIdProperty,
            IMergeNodeCommand mergeNodeCommand,
            IReplaceRelationshipsCommand replaceRelationshipsCommand)
        {
            _graphDatabase = graphDatabase;
            _contentDefinitionManager = contentDefinitionManager;
            _partSyncers = partSyncers;
            _graphSyncPartIdProperty = graphSyncPartIdProperty;
            _mergeNodeCommand = mergeNodeCommand;
            _replaceRelationshipsCommand = replaceRelationshipsCommand;
        }

        //todo: have as setting of activity, or graph sync content part settings
        private const string NcsPrefix = "ncs__";

        public async Task SyncToGraph(ContentItem contentItem)
        {
            // we use the existence of a GraphSync content part as a marker to indicate that the content item should be synced
            // so we silently noop if it's not present
            dynamic graphSyncPartContent = ((JObject) contentItem.Content)[nameof(GraphSyncPart)];
            //todo: text -> id?
            //todo: why graph sync has tags in features, others don't?
            if (graphSyncPartContent == null)
                return;

            _mergeNodeCommand.NodeLabel = NcsPrefix + contentItem.ContentType;
            // could inject _graphSyncPartIdProperty into mergeNodeCommand, but should we?
            _mergeNodeCommand.IdPropertyName = _graphSyncPartIdProperty.Name;

            var relationships = new Dictionary<(string destNodeLabel, string destIdPropertyName, string relationshipType), IEnumerable<string>>();

            await AddContentPartSyncComponents(contentItem, relationships);

            await SyncComponentsToGraph(contentItem, relationships, _graphSyncPartIdProperty.Name, _graphSyncPartIdProperty.Value(graphSyncPartContent));
        }

        private async Task AddContentPartSyncComponents(
            ContentItem contentItem,
            IDictionary<(string destNodeLabel, string destIdPropertyName, string relationshipType), IEnumerable<string>> relationships)
        {
            foreach (var partSync in _partSyncers)
            {
                string partName = partSync.PartName ?? contentItem.ContentType;

                dynamic partContent = contentItem.Content[partName];
                if (partContent == null)
                    continue;

                var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(contentItem.ContentType);
                var contentTypePartDefinition =
                    contentTypeDefinition.Parts.FirstOrDefault(p => p.PartDefinition.Name == partName);

                await partSync.AddSyncComponents(partContent, _mergeNodeCommand.Properties, relationships, contentTypePartDefinition);
            }
        }

        private async Task SyncComponentsToGraph(
            ContentItem contentItem,
            IReadOnlyDictionary<(string destNodeLabel, string destIdPropertyName, string relationshipType),IEnumerable<string>> relationships,
            string sourceIdPropertyName,
            string sourceIdPropertyValue)
        {
            string nodeLabel = NcsPrefix + contentItem.ContentType;

            List<Query> queries = new List<Query> {_mergeNodeCommand.Query};

            if (relationships.Any())
            {
                queries.Add(_replaceRelationshipsCommand.Initialise(nodeLabel, sourceIdPropertyName,
                    sourceIdPropertyValue, relationships));
            }

            await _graphDatabase.RunWriteQueries(queries.ToArray());
        }
    }
}
