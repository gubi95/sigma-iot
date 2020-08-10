namespace Sigma.IoT.Data
{
    public class AzureBlobStorageFilePathBuilder : IFilePathBuilder
    {
        public string Build(params string[] pathParts) =>
            string.Join("/", pathParts);
    }
}
