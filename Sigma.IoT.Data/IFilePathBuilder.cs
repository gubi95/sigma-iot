namespace Sigma.IoT.Data
{
    public interface IFilePathBuilder
    {
        string Build(params string[] pathParts);
    }
}
