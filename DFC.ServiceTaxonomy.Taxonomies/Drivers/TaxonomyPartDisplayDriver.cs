using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.Taxonomies.Helper;
using DFC.ServiceTaxonomy.Taxonomies.Models;
using DFC.ServiceTaxonomy.Taxonomies.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Mvc.ModelBinding;
using YesSql;

namespace DFC.ServiceTaxonomy.Taxonomies.Drivers
{
    public class TaxonomyPartDisplayDriver : ContentPartDisplayDriver<TaxonomyPart>
    {
        private readonly ITaxonomyHelper _taxonomyHelper;
        private readonly ISession _session;

        public TaxonomyPartDisplayDriver(ITaxonomyHelper taxonomyHelper, ISession session)
        {
            _taxonomyHelper = taxonomyHelper;
            _session = session;
        }

        public override IDisplayResult Display(TaxonomyPart part, BuildPartDisplayContext context)
        {
            var hasItems = part.Terms.Any();
            return Initialize<TaxonomyPartViewModel>(hasItems ? "TaxonomyPart" : "TaxonomyPart_Empty", m =>
            {
                m.ContentItem = part.ContentItem;
                m.TaxonomyPart = part;
            })
            .Location("Detail", "Content:5");
        }

        public override IDisplayResult Edit(TaxonomyPart part)
        {
            return Initialize<TaxonomyPartEditViewModel>("TaxonomyPart_Edit", model =>
            {
                model.TermContentType = part.TermContentType;
                model.TaxonomyPart = part;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(TaxonomyPart part, IUpdateModel updater)
        {
            var model = new TaxonomyPartEditViewModel();

            if (await updater.TryUpdateModelAsync(model, Prefix, t => t.Hierarchy, t => t.TermContentType))
            {
                if (!String.IsNullOrWhiteSpace(model.Hierarchy))
                {
                    var originalTaxonomyItems = part.ContentItem.As<TaxonomyPart>();

                    var newHierarchy = JArray.Parse(model.Hierarchy);      

                    var taxonomyItems = new JArray();

                    foreach (var item in newHierarchy)
                    {
                        taxonomyItems.Add(ProcessItem(originalTaxonomyItems, item as JObject));
                    }                    

                    part.Terms = taxonomyItems.ToObject<List<ContentItem>>();

                    //make sure nothing has moved that has associated pages
                    IEnumerable<ContentItem> allPages = await _session
                        .Query<ContentItem, ContentItemIndex>(x => x.ContentType == "Page" && x.Latest)
                        .ListAsync();

                    var terms = _taxonomyHelper.GetAllTerms(JObject.FromObject(part.ContentItem));

                    foreach (JObject term in terms)
                    {
                        //only bother checking further if there are any associated pages
                        if (allPages.Any(x => (string)x.Content.Page.PageLocations.TermContentItemIds[0] == (string)term["ContentItemId"]))
                        {
                            dynamic originalParent = _taxonomyHelper.FindParentTaxonomyTerm(term, JObject.FromObject(part.ContentItem));
                            dynamic newParent = _taxonomyHelper.FindParentTaxonomyTerm(term, JObject.FromObject(part));

                            if (originalParent == null || newParent == null)
                                throw new InvalidOperationException($"Could not find parent taxonomy term for {(originalParent == null ? originalParent : newParent)}");

                            if (originalParent.ContentItemId != newParent.ContentItemId)
                            {
                                updater.ModelState.AddModelError(Prefix, nameof(TaxonomyPart.Terms), "You cannot move a Page Location which has associated Pages linked to it.");
                            }
                        }
                    }
                }

                part.TermContentType = model.TermContentType;
            }

            return Edit(part);
        }

        /// <summary>
        /// Clone the content items at the specific index.
        /// </summary>
        private JObject GetTaxonomyItemAt(List<ContentItem> taxonomyItems, int[] indexes)
        {
            ContentItem taxonomyItem = null;

            // Seek the term represented by the list of indexes
            foreach (var index in indexes)
            {
                if (taxonomyItems == null || taxonomyItems.Count < index)
                {
                    // Trying to acces an unknown index
                    return null;
                }

                taxonomyItem = taxonomyItems[index];

                var terms = taxonomyItem.Content.Terms as JArray;
                taxonomyItems = terms?.ToObject<List<ContentItem>>();
            }

            var newObj = JObject.Parse(JsonConvert.SerializeObject(taxonomyItem));

            if (newObj["Terms"] != null)
            {
                newObj["Terms"] = new JArray();
            }

            return newObj;
        }

        private JObject ProcessItem(TaxonomyPart originalItems, JObject item)
        {
            var contentItem = GetTaxonomyItemAt(originalItems.Terms, item["index"].ToString().Split('-').Select(x => Convert.ToInt32(x)).ToArray());

            var children = item["children"] as JArray;

            if (children != null)
            {
                var taxonomyItems = new JArray();

                for (var i = 0; i < children.Count; i++)
                {
                    taxonomyItems.Add(ProcessItem(originalItems, children[i] as JObject));
                    contentItem["Terms"] = taxonomyItems;
                }
            }

            return contentItem;
        }
    }
}
