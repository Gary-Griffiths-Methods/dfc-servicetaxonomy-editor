using System;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.Taxonomies.Settings;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace DFC.ServiceTaxonomy.Taxonomies
{
    public class AdminMenu : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!String.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            builder.Add(S["Configuration"], configuration => configuration
                       .Add(S["Settings"], "1", settings => settings
                            .Add(S["Taxonomy Filters"], S["Taxonomy Filters"].PrefixPosition(), admt => admt
                            .AddClass("taxonomyfilters").Id("taxonomyfilters")
                                .Permission(Permissions.ManageTaxonomies)
                                .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = TaxonomyContentsAdminListSettingsDisplayDriver.GroupId })
                                .LocalNav()
                    )));

            return Task.CompletedTask;
        }
    }
}
