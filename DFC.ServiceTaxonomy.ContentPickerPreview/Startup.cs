using System;
using DFC.ServiceTaxonomy.ContentPickerPreview.Controllers;
using DFC.ServiceTaxonomy.ContentPickerPreview.Drivers;
using DFC.ServiceTaxonomy.ContentPickerPreview.Handlers;
using DFC.ServiceTaxonomy.ContentPickerPreview.Models;
using DFC.ServiceTaxonomy.ContentPickerPreview.Services;
using DFC.ServiceTaxonomy.ContentPickerPreview.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Modules;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.Mvc.Core.Utilities;

namespace DFC.ServiceTaxonomy.ContentPickerPreview
{
    //options for preview picker:
    // derive from current field?
    // use real field and tweak in ui using js?

    //custom part
    //reuse contentpicker field
    //new render field
    //bit of js?

    //can we use a widget within the admin section?

    //todo: if get copy of vue-multiselect-wrapper working for poc, use original script and listen for vue-multiselect-created event and @@input to vue-multiselect?

    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;

        static Startup()
        {
            // these are needed to be able to access the view models  in display shapes rendered by the liquid markup engine
            // TemplateContext.GlobalMemberAccessStrategy.Register<TitlePartViewModel>();
            // TemplateContext.GlobalMemberAccessStrategy.Register<TitlePartSettingsViewModel>();
        }

        public Startup(IOptions<AdminOptions> adminOptions)
        {
            _adminOptions = adminOptions.Value;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            // Title Part
            services.AddContentPart<ContentPickerPreviewPart>()
                .UseDisplayDriver<ContentPickerPreviewPartDisplay>()
                .AddHandler<ContentPickerPreviewPartHandler>();

            // services.AddScoped<IContentPartIndexHandler, ContentPickerPreviewPartIndexHandler>();
            services.AddScoped<IContentTypePartDefinitionDisplayDriver, ContentPickerPreviewPartSettingsDisplayDriver>();

            services.AddScoped<IDataMigration, Migrations>();



            services.AddScoped<IContentPickerResultProvider, PreviewContentPickerResultProvider>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            builder.UseStaticFiles();

            routes.MapAreaControllerRoute(
                name: "ContentPickerPreview",
                areaName: "DFC.ServiceTaxonomy.ContentPickerPreview",
                //todo:
                pattern: _adminOptions.AdminUrlPrefix + "/ContentPickerPreview/SearchContentItems",
                defaults: new { controller = typeof(ContentPickerPreviewAdminController).ControllerName(), action = nameof(ContentPickerPreviewAdminController.SearchContentItems) }
            );
        }
    }
    //todo: ContentLocalization?
}