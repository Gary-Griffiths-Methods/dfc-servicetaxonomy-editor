﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Helpers;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Events;
using OrchardCore.DisplayManagement.Notify;

namespace DFC.ServiceTaxonomy.GraphSync.Handlers
{
    #pragma warning disable S1186
    public class GraphSyncContentDefinitionHandler : IContentDefinitionEventHandler
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGraphResyncer _graphResyncer;
        private readonly INotifier _notifier;
        private readonly ILogger<GraphSyncContentDefinitionHandler> _logger;

        public const string FieldZombieFlag = "Zombie";

        public GraphSyncContentDefinitionHandler(
            IContentDefinitionManager contentDefinitionManager,
            IServiceProvider serviceProvider,
            IGraphResyncer graphResyncer,
            INotifier notifier,
            ILogger<GraphSyncContentDefinitionHandler> logger)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _serviceProvider = serviceProvider;
            _graphResyncer = graphResyncer;
            _notifier = notifier;
            _logger = logger;
        }

        public void ContentTypeCreated(ContentTypeCreatedContext context)
        {
        }

        public void ContentTypeRemoved(ContentTypeRemovedContext context)
        {
            try
            {
                //todo: does it need to be 2 phase?
                IDeleteGraphSyncer publishedDeleteGraphSyncer = _serviceProvider.GetRequiredService<IDeleteGraphSyncer>();
                IDeleteGraphSyncer previewDeleteGraphSyncer = _serviceProvider.GetRequiredService<IDeleteGraphSyncer>();

                // delete all nodes by type
                Task.WhenAll(
                    publishedDeleteGraphSyncer.DeleteNodesByType(GraphReplicaSetNames.Published, context.ContentTypeDefinition.Name),
                    previewDeleteGraphSyncer.DeleteNodesByType(GraphReplicaSetNames.Preview, context.ContentTypeDefinition.Name))
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                string message =
                    $"Graph resync failed after deleting the {context.ContentTypeDefinition.Name} content type.";
                _logger.LogError(e, message);
                _notifier.Add(NotifyType.Error, new LocalizedHtmlString(nameof(GraphSyncContentDefinitionHandler), message));
                throw;
            }
        }

        public void ContentTypeImporting(ContentTypeImportingContext context)
        {
        }

        public void ContentTypeImported(ContentTypeImportedContext context)
        {
        }

        public void ContentPartCreated(ContentPartCreatedContext context)
        {
        }

        public void ContentPartRemoved(ContentPartRemovedContext context)
        {
            //todo: think we need to update following a part removal, in addition to a field removal: add story
        }

        public void ContentPartAttached(ContentPartAttachedContext context)
        {
        }

        public void ContentPartDetached(ContentPartDetachedContext context)
        {
        }

        public void ContentPartImporting(ContentPartImportingContext context)
        {
        }

        public void ContentPartImported(ContentPartImportedContext context)
        {
        }

        public void ContentFieldAttached(ContentFieldAttachedContext context)
        {
        }

        public void ContentFieldDetached(ContentFieldDetachedContext context)
        {
            try
            {
                IEnumerable<ContentTypeDefinition> contentTypeDefinitions = _contentDefinitionManager.ListTypeDefinitions();
                var affectedContentTypeDefinitions = contentTypeDefinitions
                    .Where(t => t.Parts
                        .Any(p => p.PartDefinition.Name == context.ContentPartName))
                    .ToArray();

                var affectedContentTypeNames = affectedContentTypeDefinitions
                    .Select(t => t.Name);

                var affectedContentFieldDefinitions = affectedContentTypeDefinitions
                    .SelectMany(td => td.Parts)
                    .Where(pd => pd.PartDefinition.Name == context.ContentPartName)
                    .SelectMany(pd => pd.PartDefinition.Fields)
                    .Where(fd => fd.Name == context.ContentFieldName);

                foreach (var affectedContentFieldDefinition in affectedContentFieldDefinitions)
                {
                    // the content field definition isn't removed until after this event,
                    // so we set a flag not to sync the removed field
                    affectedContentFieldDefinition.Settings["ContentPartFieldSettings"]![FieldZombieFlag] = true;
                }

                foreach (string affectedContentTypeName in affectedContentTypeNames)
                {
                    _graphResyncer.ResyncContentItems(affectedContentTypeName).GetAwaiter().GetResult();
                }

            }
            catch (Exception e)
            {
                string message =
                    $"Graph resync failed after deleting the {context.ContentFieldName} field from {context.ContentPartName} parts.";
                _logger.LogError(e, message);
                _notifier.Add(NotifyType.Error, new LocalizedHtmlString(nameof(GraphSyncContentDefinitionHandler), message));
                throw;
            }
        }
        #pragma warning restore S1186
    }
}