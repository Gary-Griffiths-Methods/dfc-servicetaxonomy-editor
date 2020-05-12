﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.Events.Configuration;
using DFC.ServiceTaxonomy.Events.Models;
using DFC.ServiceTaxonomy.Events.Services.Interfaces;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;

namespace DFC.ServiceTaxonomy.Events.Activities.Tasks
{

    /// <summary>
    /// existing state      user action              server      post state         event grid events          notes
    ///                                              validation
    ///                                              passed
    /// n/a                 save draft                   y       draft
    /// n/a                 save draft                   n       n/a
    /// n/a                 publish                      y       published
    /// n/a                 publish                      n       n/a
    /// draft               save draft                   y       draft
    /// draft               save draft                   n       draft
    /// draft               publish                      y       published
    /// draft               publish                      n       draft
    /// draft               publish draft from list              published
    /// published           save draft                   y       draft+published
    /// published           save draft                   n       published
    /// published           publish                      y       published
    /// published           publish                      n       published
    /// published           publish draft from list              published           N/A                    publishing without changes is a no-op
    /// draft+published     save draft                   y       draft+published
    /// draft+published     save draft                   n       draft+published
    /// draft+published     publish                      y       published
    /// draft+published     publish                      n       draft+published
    /// draft+published     publish draft from list              published
    /// published           unpublish from list                  draft                                      new draft same as old published
    /// draft+published     unpublish from list                  draft                                      draft is unchanged
    /// draft+published     discard draft                        published                                  probably need an eg event to say discarded-draft. would be good to publish instead of deleted (& unpublished if pubed), but is it possible to figure that out?
    /// draft               delete from list
    /// published           delete from list
    /// draft+published     delete from list
    /// n/a                 import recipe (publish)
    /// </summary>
    public class PublishToEventGridTask : TaskActivity
    {
        private readonly IOptionsMonitor<EventGridConfiguration> _eventGridConfiguration;
        private readonly IEventGridContentClient _eventGridContentClient;
        private readonly IContentManager _contentManager;
        private readonly ILogger<PublishToEventGridTask> _logger;

        public PublishToEventGridTask(
            IOptionsMonitor<EventGridConfiguration> eventGridConfiguration,
            IEventGridContentClient eventGridContentClient,
            IContentManager contentManager,
            IStringLocalizer<PublishToEventGridTask> localizer,
            ILogger<PublishToEventGridTask> logger)
        {
            _eventGridConfiguration = eventGridConfiguration;
            _eventGridContentClient = eventGridContentClient;
            _contentManager = contentManager;
            _logger = logger;
            T = localizer;
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            #pragma warning disable S3220
            return Outcomes(T["Done"]);
            #pragma warning restore S3220
        }

        private IStringLocalizer T { get; }

        public override string Name => nameof(PublishToEventGridTask);
        public override LocalizedString DisplayText => T["Publish content state changes to Azure Event Grid"];
        public override LocalizedString Category => T["Event Grid"];

        public override Task<ActivityExecutionResult> ExecuteAsync(
            WorkflowExecutionContext workflowContext,
            ActivityContext activityContext)
        {
            if (!_eventGridConfiguration.CurrentValue.PublishEvents)
            {
                _logger.LogInformation("Event grid publishing is disabled. No events will be published.");
                return Task.FromResult(Outcomes("Done"));
            }

            ContentItem contentItem = (ContentItem)workflowContext.Input["ContentItem"];

            // defer processing, so that if we end up retrying the calls to event grid, we don't freeze the ui
#pragma warning disable CS4014
            ProcessEventAfterContentItemQuiesces(workflowContext, contentItem);
#pragma warning restore CS4014

            return Task.FromResult(Outcomes("Done"));
        }

        #pragma warning disable S3241
        private async Task ProcessEventAfterContentItemQuiesces(WorkflowExecutionContext workflowContext, ContentItem eventContentItem)
        {
            try
            {
                #pragma warning disable S1481
                //var preDelayContentItem = await _contentManager.CloneAsync(eventContentItem);

                var preDelayDraft = await _contentManager.GetAsync(eventContentItem.ContentItemId, VersionOptions.Draft);
                var preDelayLatest = await _contentManager.GetAsync(eventContentItem.ContentItemId, VersionOptions.Latest);
                var preDelayPublished = await _contentManager.GetAsync(eventContentItem.ContentItemId, VersionOptions.Published);

                await Task.Delay(5000);

                // session diposed. inject a session?
                //var postDelayDraft = await _contentManager.GetAsync(eventContentItem.ContentItemId, VersionOptions.Draft);
                // var postDelayLatest = await _contentManager.GetAsync(eventContentItem.ContentItemId, VersionOptions.Latest);
                //var postDelayPublished = await _contentManager.GetAsync(eventContentItem.ContentItemId, VersionOptions.Published);

                // new item failed server side validation
                if (eventContentItem.ContentItemVersionId == null)
                    return;

                string? eventType = null;
                string? eventType2 = null;

                string trigger = (string)workflowContext.Properties["Trigger"];
                switch (trigger)
                {
                    case "published":
                        //todo: when a draft item is published, the draft version goes away. either we publish an event to say draft-removed
                        // or the consumer can remove drafts on a published event
                        eventType = "published";
                        break;
                    case "updated":
                        if (!eventContentItem.Published
                            && (preDelayPublished != null
                            //todo: this stops published > save draft from publishing
                            || (preDelayDraft?.ModifiedUtc == null || eventContentItem.ModifiedUtc > preDelayDraft.ModifiedUtc)))
                        {
                            //todo: this publishes false-positive draft events when user tries to publish/draft an existing item and server side validation fails
                            //todo: this publishes false-positive draft events when user publishes a draft item (sometimes we only get the updated event, and not the published event. why?)
                            eventType = "draft";
                        }
                        break;
                    case "unpublished":    //todo: if a published only item is unpublished, it reverts to a draft version. should we publish a draft, or let the consumer subscribe to unpublish - might not be obvious??
                        // distinguish between pub+draft > unpublish and pub > unpub
                        eventType = "unpublished";
                        if (preDelayPublished.ModifiedUtc == eventContentItem.ModifiedUtc)
                        {
                            eventType2 = "draft";
                        }
                        break;
                    case "deleted":
                        //todo: do we send out unpub + undrafted, or just let the delete go.
                        // when we pub a published, should we send an undraft if there was a draft version? or let consumers sub to published, then they can remove the draft
                        // if (preDelayDraft == null)
                        // {
                        //     eventType = "unpublished";
                        // }
                        eventType = "deleted";
                        break;
                }

                //todo: modified time as event time is probably wrong. either pick correct time, or just set to now
                if (eventType != null)
                {
                    await PublishContentEvent(workflowContext, eventContentItem, eventType);
                }

                if (eventType2 != null)
                {
                    await PublishContentEvent(workflowContext, eventContentItem, eventType2);
                }
            }
            catch (Exception e)
            {
                // as we fire and forget this method, any errors won't cause the workflow to fail, so we must make sure the log can be tied back to the workflow
                _logger.LogError($"Delayed processing of workflow id {workflowContext.WorkflowId} failed: {e}");
            }
        }
        #pragma warning restore S3241

        private async Task PublishContentEvent(WorkflowExecutionContext workflowContext, ContentItem contentItem, string eventType)
        {
            // would it be better to use the workflowid as the correlation id instead?
            ContentEvent contentEvent = new ContentEvent(workflowContext.CorrelationId, contentItem, eventType);
            await _eventGridContentClient.Publish(contentEvent);
        }
    }
}
