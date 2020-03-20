using DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Interfaces;

namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers.Parts
{
    public class GraphSyncPartUriIdProperty : IGraphSyncPartIdProperty
    {
        //todo: from settings
        public string Name => "uri";

        public object Value(dynamic graphSyncContent) => graphSyncContent.Text.ToString();
    }
}