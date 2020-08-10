using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sigma.IoT.Data
{
    public interface IFileProvider
    {
        Task<Stream> GetFileAsync(string path);

        IAsyncEnumerable<Stream> GetFilesFromZipArchive(string zipArchiveBlobName);

        IReadOnlyCollection<string> ListAllFiles(string path);

        IReadOnlyCollection<string> ListAllFolders(string path);
    }
}
