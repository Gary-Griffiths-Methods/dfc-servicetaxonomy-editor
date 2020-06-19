using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.Managers.Interface;
using DFC.ServiceTaxonomy.GraphSync.Models;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers
{
    //todo: have to refactor sync. currently with bags, a single sync will occur in multiple transactions
    // so if a validation fails for example, the graph will be left in an incomplete synced state
    // need to gather up all commands, then execute them in a single transaction
    // giving the commands the opportunity to validate the results before the transaction is committed
    // so any validation failure rolls back the whole sync operation
    public class MergeGraphSyncer : IMergeGraphSyncer
    {
        private readonly IGraphDatabase _graphDatabase;
        private readonly ICustomContentDefintionManager _contentDefinitionManager;
        private readonly IEnumerable<IContentPartGraphSyncer> _partSyncers;
        private readonly IGraphSyncHelper _graphSyncHelper;
        private readonly IMergeNodeCommand _mergeNodeCommand;
        private readonly IReplaceRelationshipsCommand _replaceRelationshipsCommand;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MergeGraphSyncer> _logger;

        public MergeGraphSyncer(
            IGraphDatabase graphDatabase,
            ICustomContentDefintionManager contentDefinitionManager,
            IEnumerable<IContentPartGraphSyncer> partSyncers,
            IGraphSyncHelper graphSyncHelper,
            IMergeNodeCommand mergeNodeCommand,
            IReplaceRelationshipsCommand replaceRelationshipsCommand,
            IMemoryCache memoryCache,
            ILogger<MergeGraphSyncer> logger)
        {
            _graphDatabase = graphDatabase;
            _contentDefinitionManager = contentDefinitionManager;
            _partSyncers = partSyncers;
            _graphSyncHelper = graphSyncHelper;
            _mergeNodeCommand = mergeNodeCommand;
            _replaceRelationshipsCommand = replaceRelationshipsCommand;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<IMergeNodeCommand?> SyncToGraph(ContentItem contentItem)
        {
            // we use the existence of a GraphSync content part as a marker to indicate that the content item should be synced
            // so we silently noop if it's not present
            JObject? graphSyncPartContent = (JObject?)contentItem.Content[nameof(GraphSyncPart)];
            //todo: text -> id?
            //todo: why graph sync has tags in features, others don't?
            if (graphSyncPartContent == null)
                return null;

            string? disableSyncContentItemVersionId = _memoryCache.Get<string>($"DisableSync_{contentItem.ContentItemVersionId}");
            if (disableSyncContentItemVersionId != null)
            {
                _logger.LogInformation($"Not syncing {contentItem.ContentType}:{contentItem.ContentItemId}, version {disableSyncContentItemVersionId} as syncing has been disabled for it");
                return null;
            }

            _logger.LogDebug($"Syncing {contentItem.ContentType} : {contentItem.ContentItemId}");

            _graphSyncHelper.ContentType = contentItem.ContentType;

            _mergeNodeCommand.NodeLabels.UnionWith(await _graphSyncHelper.NodeLabels());
            _mergeNodeCommand.IdPropertyName = _graphSyncHelper.IdPropertyName();

            //Add created and modified dates to all content items
            //todo: store as neo's DateTime? especially if api doesn't match the string format
            if (contentItem.CreatedUtc.HasValue)
                _mergeNodeCommand.Properties.Add(await _graphSyncHelper.PropertyName("CreatedDate"), contentItem.CreatedUtc.Value);

            if (contentItem.ModifiedUtc.HasValue)
                _mergeNodeCommand.Properties.Add(await _graphSyncHelper.PropertyName("ModifiedDate"), contentItem.ModifiedUtc.Value);

            await AddContentPartSyncComponents(contentItem.ContentType, contentItem.Content);

            _logger.LogInformation($"Syncing {contentItem.ContentType} : {contentItem.ContentItemId} to {_mergeNodeCommand}");

            await SyncComponentsToGraph(graphSyncPartContent);

            return _mergeNodeCommand;
        }

        private async Task AddContentPartSyncComponents(
            string contentType,
            JObject content)
        {
            // ensure graph sync part is processed first, as other part syncers (current bagpart) require the node's id value
            string graphSyncPartName = nameof(GraphSyncPart);
            var partSyncersWithGraphLookupFirst
                = _partSyncers.Where(ps => ps.PartName != graphSyncPartName)
                    .Prepend(_partSyncers.First(ps => ps.PartName == graphSyncPartName));

            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(contentType);

            foreach (var partSync in partSyncersWithGraphLookupFirst)
            {
                // bag part has p.Name == <<name>>, p.PartDefinition.Name == "BagPart"
                // (other non-named parts have the part name in both)

                var contentTypePartDefinitions =
                    contentTypeDefinition.Parts.Where(p => partSync.CanHandle(contentType, p.PartDefinition));

                foreach (var contentTypePartDefinition in contentTypePartDefinitions)
                {
                    string namedPartName = contentTypePartDefinition.Name;

                    dynamic? partContent = content[namedPartName];
                    if (partContent == null)
                        continue; //todo: throw??

                    await partSync.AddSyncComponents(
                        partContent,
                        _mergeNodeCommand,
                        _replaceRelationshipsCommand,
                        contentTypePartDefinition,
                        _graphSyncHelper);
                }
            }
        }

        private async Task SyncComponentsToGraph(dynamic graphSyncPartContent)
        {
            List<ICommand> commands = new List<ICommand>();

            if (!_graphSyncHelper.GraphSyncPartSettings.PreexistingNode)
            {
                commands.Add(_mergeNodeCommand);
            }

            if (_replaceRelationshipsCommand.Relationships.Any())
            {
                // doesn't really belong here...
                _replaceRelationshipsCommand.SourceNodeLabels = new HashSet<string>(_mergeNodeCommand.NodeLabels);
                _replaceRelationshipsCommand.SourceIdPropertyName = _mergeNodeCommand.IdPropertyName;
                _replaceRelationshipsCommand.SourceIdPropertyValue = _graphSyncHelper.GetIdPropertyValue(graphSyncPartContent);

                commands.Add(_replaceRelationshipsCommand);
            }

            await _graphDatabase.Run(commands.ToArray());
        }
    }
}
