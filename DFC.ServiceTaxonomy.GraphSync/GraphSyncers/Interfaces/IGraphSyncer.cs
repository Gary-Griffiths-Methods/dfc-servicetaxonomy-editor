using System;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using Newtonsoft.Json.Linq;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces
{
    public interface IGraphSyncer
    {
        Task<IMergeNodeCommand?> SyncToGraph(string contentType, JObject content, DateTime? createdUtc, DateTime? modifiedUtc);
    }
}