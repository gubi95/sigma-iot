namespace Sigma.IoT.DataCollector
{
    internal sealed class Configuration
    {
        public string BlobEndpoint { get; set; }

        public string BlobContainerName { get; set; }

        public string CacheEndpoint { get; set; }

        public string CacheDbName { get; set; }

        public string CacheCollectionName { get; set; }
    }
}
