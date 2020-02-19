namespace DFC.ServiceTaxonomy.GraphSync.GraphSyncers
{
    public class GraphSyncPartUriIdProperty : IGraphSyncPartIdProperty
    {
        //todo: from settings
        public string Name => "uri";

        public string Value(dynamic graphSyncContent) => graphSyncContent.Text.ToString();
    }
}