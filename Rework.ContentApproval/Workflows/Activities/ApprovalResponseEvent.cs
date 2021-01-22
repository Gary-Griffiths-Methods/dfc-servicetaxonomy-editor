﻿using Microsoft.Extensions.Localization;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using System.Collections.Generic;

namespace Rework.ContentApproval.Workflows.Activities
{
    public class ApprovalResponseEvent : EventActivity
    {
        public static string EventName => nameof(ApprovalResponseEvent);

        private readonly IStringLocalizer S;

        public ApprovalResponseEvent(
            IStringLocalizer<ApprovalResponseEvent> localizer
        )
        {
            S = localizer;
        }

        public override string Name => EventName;
        public override LocalizedString DisplayText => S["Approval Response Event"];
        public override LocalizedString Category => S["Content"];

        public override bool CanExecute(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return true;
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(S["Done"]);
        }

        public override ActivityExecutionResult Resume(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes("Done");
        }
    }
}
