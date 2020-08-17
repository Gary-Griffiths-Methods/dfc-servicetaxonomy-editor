﻿using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.Extensions;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Contexts;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.EmbeddedContentItemsGraphSyncer;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Parts;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Results.AllowSync;
using Newtonsoft.Json.Linq;
using DFC.ServiceTaxonomy.Taxonomies.Models;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts.Taxonomy
{
    public class TaxonomyPartGraphSyncer : ContentPartGraphSyncer, ITaxonomyPartGraphSyncer
    {
        private readonly ITaxonomyPartEmbeddedContentItemsGraphSyncer _taxonomyPartEmbeddedContentItemsGraphSyncer;
        public override string PartName => nameof(TaxonomyPart);

        private const string ContainerName = "Terms";
        internal const string TermContentTypePropertyName = "TermContentType";

        public TaxonomyPartGraphSyncer(
            ITaxonomyPartEmbeddedContentItemsGraphSyncer taxonomyPartEmbeddedContentItemsGraphSyncer)
        {
            _taxonomyPartEmbeddedContentItemsGraphSyncer = taxonomyPartEmbeddedContentItemsGraphSyncer;
        }

        public override async Task AllowSync(JObject content, IGraphMergeContext context, IAllowSyncResult allowSyncResult)
        {
            await _taxonomyPartEmbeddedContentItemsGraphSyncer.AllowSync((JArray?)content[ContainerName], context, allowSyncResult);
        }

        public override async Task AddSyncComponents(JObject content, IGraphMergeContext context)
        {
            context.MergeNodeCommand.AddProperty<string>(TermContentTypePropertyName, content);

            _taxonomyPartEmbeddedContentItemsGraphSyncer.IsNonLeafEmbeddedTerm = false;
            await _taxonomyPartEmbeddedContentItemsGraphSyncer.AddSyncComponents((JArray?)content[ContainerName], context);
        }

        public async Task AddSyncComponentsForNonLeafEmbeddedTerm(JObject content, IGraphMergeContext context)
        {
            _taxonomyPartEmbeddedContentItemsGraphSyncer.IsNonLeafEmbeddedTerm = true;
            await _taxonomyPartEmbeddedContentItemsGraphSyncer.AddSyncComponents((JArray?)content[ContainerName], context);
        }

        public override async Task AllowDelete(JObject content, IGraphDeleteContext context, IAllowSyncResult allowSyncResult)
        {
            await _taxonomyPartEmbeddedContentItemsGraphSyncer.AllowDelete((JArray?)content[ContainerName], context, allowSyncResult);
        }

        public override async Task DeleteComponents(JObject content, IGraphDeleteContext context)
        {
            _taxonomyPartEmbeddedContentItemsGraphSyncer.IsNonLeafEmbeddedTerm = false;
            await _taxonomyPartEmbeddedContentItemsGraphSyncer.DeleteComponents((JArray?)content[ContainerName], context);
        }

        public async Task DeleteComponentsForNonLeafEmbeddedTerm(JObject content, IGraphDeleteContext context)
        {
            _taxonomyPartEmbeddedContentItemsGraphSyncer.IsNonLeafEmbeddedTerm = true;
            await _taxonomyPartEmbeddedContentItemsGraphSyncer.DeleteComponents((JArray?)content[ContainerName], context);
        }

        //todo: we now need to validate any 2 way incoming relationships we created
        public override async Task<(bool validated, string failureReason)> ValidateSyncComponent(
            JObject content,
            IValidateAndRepairContext context)
        {
            (bool validated, string failureReason) =
                await _taxonomyPartEmbeddedContentItemsGraphSyncer.ValidateSyncComponent(
                    (JArray?)content[ContainerName], context);

            if (!validated)
                return (validated, failureReason);

            return context.GraphValidationHelper.StringContentPropertyMatchesNodeProperty(
                TermContentTypePropertyName,
                content,
                TermContentTypePropertyName,
                context.NodeWithOutgoingRelationships.SourceNode);
        }
    }
}
