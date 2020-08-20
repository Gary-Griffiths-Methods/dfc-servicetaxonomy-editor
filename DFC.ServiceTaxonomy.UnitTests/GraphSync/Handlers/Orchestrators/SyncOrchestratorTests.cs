﻿using System;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.ContentItemVersions;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Contexts;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces.Results.AllowSync;
using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Results.AllowSync;
using DFC.ServiceTaxonomy.GraphSync.Handlers.Orchestrators;
using DFC.ServiceTaxonomy.Neo4j.Services.Interfaces;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.Notify;
using Xunit;

namespace DFC.ServiceTaxonomy.UnitTests.GraphSync.Handlers.Orchestrators
{
    public class SyncOrchestratorTests
    {
        public SyncOrchestrator SyncOrchestrator { get; set; }
        public IContentDefinitionManager ContentDefinitionManager { get; set; }
        public INotifier Notifier { get; set; }
        public IGraphCluster GraphCluster { get; set; }
        public IPublishedContentItemVersion PublishedContentItemVersion { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public ILogger<SyncOrchestrator> Logger { get; set; }

        public ContentItem ContentItem { get; set; }
        public IMergeGraphSyncer MergeGraphSyncer { get; set; }
        public IAllowSyncResult PreviewAllowSyncResult { get; set; }
        public IAllowSyncResult PublishedAllowSyncResult { get; set; }

        public SyncOrchestratorTests()
        {
            ContentDefinitionManager = A.Fake<IContentDefinitionManager>();
            Notifier = A.Fake<Notifier>();
            GraphCluster = A.Fake<IGraphCluster>();
            PublishedContentItemVersion = A.Fake<IPublishedContentItemVersion>();
            ServiceProvider = A.Fake<IServiceProvider>();
            Logger = A.Fake<ILogger<SyncOrchestrator>>();

            ContentItem = new ContentItem();

            MergeGraphSyncer = A.Fake<IMergeGraphSyncer>();
            A.CallTo(() => ServiceProvider.GetRequiredService<IMergeGraphSyncer>())
                .Returns(MergeGraphSyncer);

            PreviewAllowSyncResult = A.Fake<IAllowSyncResult>();
            A.CallTo(() => MergeGraphSyncer.SyncAllowed(
                    A<IGraphReplicaSet>.That.Matches(s => s.Name == "Preview"),
                    A<ContentItem>._, A<IContentManager>._, A<IGraphMergeContext?>._))
                .Returns(PreviewAllowSyncResult);

            PublishedAllowSyncResult = A.Fake<IAllowSyncResult>();
            A.CallTo(() => MergeGraphSyncer.SyncAllowed(
                    A<IGraphReplicaSet>.That.Matches(s => s.Name == "Published"),
                    A<ContentItem>._, A<IContentManager>._, A<IGraphMergeContext?>._))
                .Returns(PublishedAllowSyncResult);

            SyncOrchestrator = new SyncOrchestrator(
                ContentDefinitionManager,
                Notifier,
                GraphCluster,
                PublishedContentItemVersion,
                ServiceProvider,
                Logger);
        }

        // [Theory]
        // [InlineData(SyncStatus.Allowed, false, true)]
        // [InlineData(SyncStatus.Allowed, true, false)]
        // [InlineData(SyncStatus.Blocked, null, false)]
        // [InlineData(SyncStatus.NotRequired, null, true)]
        public async Task SaveDraft_SyncAllowedSyncMatrix_ReturnsBool(SyncStatus syncAllowedStatus, bool? syncToGraphReplicaSetThrows, bool expectedSuccess)
        {
            A.CallTo(() => PreviewAllowSyncResult.AllowSync)
                .Returns(syncAllowedStatus);

            if (syncToGraphReplicaSetThrows == true)
            {
                A.CallTo(() => MergeGraphSyncer.SyncToGraphReplicaSet())
                    .Throws(() => new Exception());
            }

            bool success = await SyncOrchestrator.SaveDraft(ContentItem);

            Assert.Equal(expectedSuccess, success);

            if (syncToGraphReplicaSetThrows == null)
            {
                A.CallTo(() => MergeGraphSyncer.SyncToGraphReplicaSet())
                    .MustNotHaveHappened();
            }
        }
    }
}
