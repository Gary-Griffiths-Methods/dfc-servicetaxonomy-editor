using System;
using DFC.ServiceTaxonomy.Editor.Module.Drivers;
using DFC.ServiceTaxonomy.GraphSync.Activities;
using DFC.ServiceTaxonomy.GraphSync.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using DFC.ServiceTaxonomy.GraphSync.Drivers;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts;
using DFC.ServiceTaxonomy.GraphSync.Handlers;
using DFC.ServiceTaxonomy.GraphSync.Models;
using DFC.ServiceTaxonomy.GraphSync.Settings;
using DFC.ServiceTaxonomy.Neo4j.Commands;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Configuration;
using DFC.ServiceTaxonomy.Neo4j.Log;
using DFC.ServiceTaxonomy.Neo4j.Services;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver;
using OrchardCore.Modules;
using OrchardCore.Workflows.Helpers;
using DFC.ServiceTaxonomy.GraphSync.Activities.Events;
using DFC.ServiceTaxonomy.GraphSync.Drivers.Events;
using DFC.ServiceTaxonomy.GraphSync.Services;
using DFC.ServiceTaxonomy.GraphSync.Notifications;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.ContentTypes.Services;
using DFC.ServiceTaxonomy.GraphSync.Services.Interface;

namespace DFC.ServiceTaxonomy.GraphSync
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();

            services.Configure<Neo4jConfiguration>(configuration.GetSection("Neo4j"));
            services.Configure<NamespacePrefixConfiguration>(configuration.GetSection("GraphSync"));

            // Graph Database
            services.AddTransient<ILogger, NeoLogger>();
            services.AddSingleton<IGraphDatabase, NeoGraphDatabase>();
            services.AddTransient<IMergeNodeCommand, MergeNodeCommand>();
            services.AddTransient<IDeleteNodeCommand, DeleteNodeCommand>();
            services.AddTransient<IDeleteNodesByTypeCommand, DeleteNodesByTypeCommand>();
            services.AddTransient<IReplaceRelationshipsCommand, ReplaceRelationshipsCommand>();

            // Sync to graph workflow task
            services.AddActivity<SyncToGraphTask, SyncToGraphTaskDisplay>();
            services.AddActivity<DeleteFromGraphTask, DeleteFromGraphTaskDisplay>();
            services.AddActivity<DeleteContentTypeFromGraphTask, DeleteContentTypeFromGraphTaskDisplay>();
            services.AddActivity<ContentTypeDeletedEvent, ContentTypeDeletedEventDisplay>();
            services.AddActivity<DeleteContentTypeTask, DeleteContentTypeTaskDisplay>();

            // Syncers
            services.AddTransient<IMergeGraphSyncer, MergeGraphSyncer>();
            services.AddTransient<IDeleteGraphSyncer, DeleteGraphSyncer>();
            services.AddTransient<IContentPartGraphSyncer, TitlePartGraphSyncer>();
            services.AddTransient<IContentPartGraphSyncer, BagPartGraphSyncer>();
            services.AddTransient<IContentPartGraphSyncer, EponymousPartGraphSyncer>();
            services.AddTransient<IGraphSyncPartIdProperty, GraphSyncPartUriIdProperty>();

            // Graph Sync Part
            services.AddContentPart<GraphSyncPart>()
                .UseDisplayDriver<GraphSyncPartDisplayDriver>()
                .AddHandler<GraphSyncPartHandler>();
            services.AddScoped<IContentTypePartDefinitionDisplayDriver, GraphSyncPartSettingsDisplayDriver>();
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentPartGraphSyncer, GraphSyncPartGraphSyncer>();

            //Notifiers
            services.AddScoped<INotifier, CustomNotifier>();

            //Services
            services.AddScoped<IOrchardCoreContentDefinitionService, OrchardCoreContentDefinitionService>();
            services.AddScoped<IContentDefinitionService, CustomContentDefinitionService>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Home",
                areaName: "DFC.ServiceTaxonomy.GraphSync",
                pattern: "Home/Index",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}
