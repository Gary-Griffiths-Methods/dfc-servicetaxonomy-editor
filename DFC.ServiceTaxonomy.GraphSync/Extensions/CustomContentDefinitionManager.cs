﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Environment.Cache;
using System.Linq;
using DFC.ServiceTaxonomy.GraphSync.Activities;
using System;
using OrchardCore.Workflows.Services;

namespace DFC.ServiceTaxonomy.GraphSync.Extensions
{
    public class CustomContentDefinitionManager : IContentDefinitionManager
    {
        private readonly ContentDefinitionManager _ocContentDefinitionManager;
        private readonly IWorkflowManager _workflowManager;

        public CustomContentDefinitionManager(
            ISignal signal,
            IContentDefinitionStore contentDefinitionStore,
            IMemoryCache memoryCache,
            IWorkflowManager workflowManager)
        {
            _ocContentDefinitionManager = new ContentDefinitionManager(signal, contentDefinitionStore, memoryCache);
            _workflowManager = workflowManager;
        }

        public IChangeToken ChangeToken => throw new System.NotImplementedException();

        public void DeletePartDefinition(string name)
        {
            _ocContentDefinitionManager.DeletePartDefinition(name);
        }

        public void DeleteTypeDefinition(string name)
        {
            _ocContentDefinitionManager.DeleteTypeDefinition(name);
        }

        public ContentPartDefinition GetPartDefinition(string name)
        {
            return _ocContentDefinitionManager.GetPartDefinition(name);
        }

        public ContentTypeDefinition GetTypeDefinition(string name)
        {
            return _ocContentDefinitionManager.GetTypeDefinition(name);
        }

        public Task<int> GetTypesHashAsync()
        {
            return _ocContentDefinitionManager.GetTypesHashAsync();
        }

        public IEnumerable<ContentPartDefinition> ListPartDefinitions()
        {
            return _ocContentDefinitionManager.ListPartDefinitions();
        }

        public IEnumerable<ContentTypeDefinition> ListTypeDefinitions()
        {
            return _ocContentDefinitionManager.ListTypeDefinitions();
        }

        public ContentPartDefinition LoadPartDefinition(string name)
        {
            return _ocContentDefinitionManager.LoadPartDefinition(name);
        }

        public IEnumerable<ContentPartDefinition> LoadPartDefinitions()
        {
            return _ocContentDefinitionManager.LoadPartDefinitions();
        }

        public ContentTypeDefinition LoadTypeDefinition(string name)
        {
            return _ocContentDefinitionManager.LoadTypeDefinition(name);
        }

        public IEnumerable<ContentTypeDefinition> LoadTypeDefinitions()
        {
            return _ocContentDefinitionManager.LoadTypeDefinitions();
        }

        public void StorePartDefinition(ContentPartDefinition contentPartDefinition)
        {
            var addedFields = new List<string>();
            var removedFields = new List<string>();

            var existingPartDefinition = this.GetPartDefinition(contentPartDefinition.Name);

            _ocContentDefinitionManager.StorePartDefinition(contentPartDefinition);

            foreach (var partField in contentPartDefinition.Fields)
            {
                var existingField = existingPartDefinition.Fields.FirstOrDefault(x => x.Name == partField.Name);

                if (existingField == null)
                {
                    //New field has been added
                    addedFields.Add(partField.Name);
                }

            }

            foreach (var partField in existingPartDefinition.Fields)
            {
                var newField = contentPartDefinition.Fields.FirstOrDefault(x => x.Name == partField.Name);

                if (newField == null)
                {
                    //Old field has been removed
                    removedFields.Add(partField.Name);
                }
            }

            _workflowManager.TriggerEventAsync(nameof(ContentTypeUpdatedEvent), new { ContentType = contentPartDefinition.Name, Added = addedFields, Removed = removedFields }, contentPartDefinition.Name).GetAwaiter().GetResult();
        }

        public void StoreTypeDefinition(ContentTypeDefinition contentTypeDefinition)
        {
            var addedFields = new List<string>();
            var removedFields = new List<string>();

            var existingDefinition = this.GetTypeDefinition(contentTypeDefinition.Name);

            foreach (var partField in contentTypeDefinition.Parts.SelectMany(z => z.PartDefinition.Fields))
            {
                var existingField = existingDefinition.Parts.SelectMany(z => z.PartDefinition.Fields).FirstOrDefault(x => x.Name == partField.Name);

                if (existingField == null)
                {
                    //New field has been added
                    addedFields.Add(partField.Name);
                }

            }

            foreach (var partField in existingDefinition.Parts.SelectMany(z => z.PartDefinition.Fields))
            {
                var newField = contentTypeDefinition.Parts.SelectMany(z => z.PartDefinition.Fields).FirstOrDefault(x => x.Name == partField.Name);

                if (newField == null)
                {
                    //Old field has been removed
                    removedFields.Add(partField.Name);
                }
            }

            _ocContentDefinitionManager.StoreTypeDefinition(contentTypeDefinition);
            _workflowManager.TriggerEventAsync(nameof(ContentTypeUpdatedEvent), new { ContentType = contentTypeDefinition.Name, Added = addedFields, Removed = removedFields }, contentTypeDefinition.Name).GetAwaiter().GetResult();
        }

    }
}
