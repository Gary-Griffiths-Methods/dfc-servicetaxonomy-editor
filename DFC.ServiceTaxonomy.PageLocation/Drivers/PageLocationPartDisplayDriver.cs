﻿using System.Threading.Tasks;
using DFC.ServiceTaxonomy.PageLocation.Models;
using DFC.ServiceTaxonomy.PageLocation.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using System.Linq;
using OrchardCore.Mvc.ModelBinding;
using YesSql;
using OrchardCore.ContentManagement;
using DFC.ServiceTaxonomy.PageLocation.Indexes;
using System.Collections.Generic;

namespace DFC.ServiceTaxonomy.PageLocation.Drivers
{
    public class PageLocationPartDisplayDriver : ContentPartDisplayDriver<PageLocationPart>
    {
        public static char[] InvalidCharactersForPath = ":?#[]@!$&'()*+,.;=<>\\/|%".ToCharArray();

        private readonly ISession _session;

        public PageLocationPartDisplayDriver(ISession session)
        {
            _session = session;
        }

        public override IDisplayResult Edit(PageLocationPart pageLocationPart, BuildPartEditorContext context)
        {
            return Initialize<PageLocationPartViewModel>("PageLocationPart_Edit", model =>
            {
                model.UrlName = pageLocationPart.UrlName;
                model.DefaultPageForLocation = pageLocationPart.DefaultPageForLocation;
                model.FullUrl = pageLocationPart.FullUrl;
                model.RedirectLocations = pageLocationPart.RedirectLocations;
                model.PageLocationPart = pageLocationPart;
                model.ContentItem = pageLocationPart.ContentItem;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(PageLocationPart model, IUpdateModel updater, UpdatePartEditorContext context)
        {
            await updater.TryUpdateModelAsync(model, Prefix, t => t.UrlName, t => t.DefaultPageForLocation, t => t.RedirectLocations, t => t.FullUrl);

            await ValidateAsync(model, updater);

            return Edit(model, context);
        }

        private async Task ValidateAsync(PageLocationPart pageLocation, IUpdateModel updater)
        {
            if (string.IsNullOrWhiteSpace(pageLocation.UrlName))
            {
                updater.ModelState.AddModelError(Prefix, nameof(pageLocation.UrlName), "A value is required for 'UrlName'");
            }

            if (pageLocation.UrlName?.IndexOfAny(InvalidCharactersForPath) > -1 || pageLocation.UrlName?.IndexOf(' ') > -1)
            {
                var invalidCharactersForMessage = string.Join(", ", InvalidCharactersForPath.Select(c => $"\"{c}\""));
                updater.ModelState.AddModelError(Prefix, nameof(pageLocation.UrlName), $"Please do not use any of the following characters in your URL name: {invalidCharactersForMessage}. No spaces are allowed (please use dashes or underscores instead).");
            }

            var otherPages = await _session.Query<ContentItem, PageLocationPartIndex>(x => x.ContentItemId != pageLocation.ContentItem.ContentItemId).ListAsync();

            if (otherPages.Any(x => x.ContentItemId != pageLocation.ContentItem.ContentItemId && x.Content.PageLocationPart.FullUrl == pageLocation.FullUrl))
            {
                updater.ModelState.AddModelError(Prefix, nameof(pageLocation.UrlName), "There is already a page with this URL Name at the same Page Location. Please choose a different URL Name, or change the Page Location.");
            }

            var redirectLocations = pageLocation.RedirectLocations?.Split("\r\n").ToList();

            if (redirectLocations?.Any() ?? false)
            {
                foreach (var otherPage in otherPages)
                {
                    List<string> otherPageRedirectLocations = ((string)otherPage.Content.PageLocationPart.RedirectLocations)?.Split("\r\n").ToList() ?? new List<string>();

                    var redirectConflict = otherPageRedirectLocations.FirstOrDefault(x => redirectLocations.Any(y => y == x));

                    if (redirectConflict != null)
                    {                        
                        updater.ModelState.AddModelError(Prefix, nameof(pageLocation.RedirectLocations), $"'{redirectConflict}' has already been used as a redirect location for another page.'");
                        break;
                    }

                    var fullUrlConflict = redirectLocations.FirstOrDefault(x => x == (string)otherPage.Content.PageLocationPart.FullUrl);

                    if (fullUrlConflict != null)
                    {
                        updater.ModelState.AddModelError(Prefix, nameof(pageLocation.RedirectLocations), $"'{fullUrlConflict}' has already been used as the URL for another page.'");
                        break;
                    }
                }
            }
        }
    }
}