﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Queries.Interfaces;

namespace DFC.ServiceTaxonomy.Neo4j.Services.Interfaces
{
    public interface IGraphReplicaSet
    {
        string Name { get; }
        int InstanceCount { get; }

        /// <summary>
        /// Run a query against a replica.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="instance">If supplied, the query will be run against this instance.
        /// If not supplied, queries will round-robin.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<List<T>> Run<T>(IQuery<T> query, int? instance = null);

        /// <summary>
        /// Run commands, in order, within a write transaction, against all replicas in the set. No results returned.
        /// </summary>
        /// <param name="commands">The command(s) to run.</param>
        /// <returns></returns>
        Task Run(params ICommand[] commands);    // force all?
    }
}