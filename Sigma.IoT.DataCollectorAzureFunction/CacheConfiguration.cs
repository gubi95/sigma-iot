namespace Sigma.IoT.DataCollectorAzureFunction
{
    internal sealed class CacheConfiguration
    {
        public string Endpoint { get; set; }

        public string DbName { get; set; }

        public string CollectionName { get; set; }
    }
}
