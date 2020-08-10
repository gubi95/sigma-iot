using System.IO;
using Sigma.IoT.Data;

namespace Sigma.IoT.Tests.Shared
{
    internal sealed class TestFilePathBuilder : IFilePathBuilder
    {
        public string Build(params string[] pathParts) => 
            Path.Combine(pathParts);
    }
}
