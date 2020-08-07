﻿using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Contexts;
using DFC.ServiceTaxonomy.GraphSync.OrchardCore.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.OrchardCore.Wrappers;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Contexts
{
    public class GraphOperationContext : IGraphOperationContext
    {
        public IContentManager ContentManager { get; }
        public IContentItemVersion ContentItemVersion { get; protected set; }
        public ContentTypePartDefinition ContentTypePartDefinition { get; set; }
        public IContentPartFieldDefinition? ContentPartFieldDefinition { get; private set; }

        public IGraphSyncHelper GraphSyncHelper { get; }

        protected GraphOperationContext(
            IGraphSyncHelper graphSyncHelper,
            IContentManager contentManager)
        {
            GraphSyncHelper = graphSyncHelper;
            ContentManager = contentManager;

            // needs to be set by the derived class: how to enforce?
            ContentItemVersion = default!;
            // will be set before any syncers receive a context
            ContentTypePartDefinition = default!;
        }

        public void SetContentPartFieldDefinition(ContentPartFieldDefinition? contentPartFieldDefinition)
        {
            ContentPartFieldDefinition = contentPartFieldDefinition != null
                ? new ContentPartFieldDefinitionWrapper(contentPartFieldDefinition) : default;
        }
    }
}