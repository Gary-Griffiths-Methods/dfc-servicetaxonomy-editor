﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.Configuration;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using DFC.ServiceTaxonomy.GraphSync.Models;
using DFC.ServiceTaxonomy.GraphSync.Settings;
using DFC.ServiceTaxonomy.GraphSync.ViewModels;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement.Metadata;

namespace DFC.ServiceTaxonomy.GraphSync.Drivers
{
    public class GraphSyncPartDisplayDriver : ContentPartDisplayDriver<GraphSyncPart>
    {
        private readonly IOptionsSnapshot<NamespacePrefixConfiguration> _namespacePrefixOptions;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public GraphSyncPartDisplayDriver(
            IOptionsSnapshot<NamespacePrefixConfiguration> namespacePrefixOptions,
            IContentDefinitionManager contentDefinitionManager)
        {
            _namespacePrefixOptions = namespacePrefixOptions;
            _contentDefinitionManager = contentDefinitionManager;
        }

        // public override IDisplayResult Display(GraphSyncPart GraphSyncPart)
        // {
        //     return Combine(
        //         Initialize<GraphSyncPartViewModel>("GraphSyncPart", m => BuildViewModel(m, GraphSyncPart))
        //             .Location("Detail", "Content:20"),
        //         Initialize<GraphSyncPartViewModel>("GraphSyncPart_Summary", m => BuildViewModel(m, GraphSyncPart))
        //             .Location("Summary", "Meta:5")
        //     );
        // }

        public override IDisplayResult Edit(GraphSyncPart graphSyncPart) //, BuildPartEditorContext context)
        {
            return Initialize<GraphSyncPartViewModel>("GraphSyncPart_Edit", m => BuildViewModel(m, graphSyncPart));
        }

        public override async Task<IDisplayResult> UpdateAsync(GraphSyncPart model, IUpdateModel updater)
        {
            await updater.TryUpdateModelAsync(model, Prefix, t => t.Text);

            return Edit(model);
        }

        //todo: easier to get from context?
        public GraphSyncPartSettings GetGraphSyncPartSettings(GraphSyncPart part)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(part.ContentItem.ContentType);
            var contentTypePartDefinition = contentTypeDefinition.Parts.FirstOrDefault(p => p.PartDefinition.Name == nameof(GraphSyncPart));
            return contentTypePartDefinition.GetSettings<GraphSyncPartSettings>();
        }

        private void BuildViewModel(GraphSyncPartViewModel model, GraphSyncPart part)
        {
            model.Text = part.Text ?? GenerateUriId(part);
        }

        private string GenerateUriId(GraphSyncPart part)
        {
            var settings = GetGraphSyncPartSettings(part);

            string namespacePrefix = settings.NamespacePrefix ??
                                     _namespacePrefixOptions.Value.NamespacePrefixOptions.FirstOrDefault();

            return $"{namespacePrefix}{part.ContentItem.ContentType.ToLowerInvariant()}/{Guid.NewGuid():D}";
        }
    }
}
