using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace Sigma.IoT.Data
{
    public sealed class AzureBlobStorageFileProvider : IFileProvider
    {
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly string _containerName;

        public AzureBlobStorageFileProvider(CloudBlobClient cloudBlobClient, string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException($"Argument {nameof(containerName)} cannot be null or empty string");
            }

            _cloudBlobClient = cloudBlobClient ?? throw new ArgumentNullException(nameof(cloudBlobClient));
            _containerName = containerName;
        }

        public async Task<Stream> GetFileAsync(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException($"Argument {blobName} cannot be null or empty string");
            }

            var result = await GetFileAsync(blobName, BlobType.AppendBlob, false).ConfigureAwait(false);
            return result;
        }

        public async IAsyncEnumerable<Stream> GetFilesFromZipArchive(string zipArchiveBlobName)
        {
            if (string.IsNullOrWhiteSpace(zipArchiveBlobName))
            {
                throw new ArgumentException($"Argument {zipArchiveBlobName} cannot be null or empty string");
            }

            var stream = await GetFileAsync(zipArchiveBlobName, BlobType.BlockBlob, true).ConfigureAwait(false);

            if (stream != null)
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
                foreach (var zipArchiveEntry in archive.Entries)
                {
                    Stream archiveFileStream;

                    try
                    {
                        // Some .csv file in zip has invalid header
                        archiveFileStream = zipArchiveEntry.Open();
                    }
                    catch
                    {
                        continue;
                    }

                    yield return archiveFileStream;
                }
            }
        }

        public IReadOnlyCollection<string> ListAllFiles(string path)
        {
            var container = _cloudBlobClient.GetContainerReference(_containerName);
            var blobs = container.GetDirectoryReference(path).ListBlobs().OfType<CloudBlob>().ToList();
            return blobs.Select(blob => blob.Name.Split("/").Last()).ToList();
        }

        public IReadOnlyCollection<string> ListAllFolders(string path)
        {
            var container = _cloudBlobClient.GetContainerReference(_containerName);
            var blobs = container.ListBlobs(path)
                .OfType<CloudBlobDirectory>()
                .ToList();
            return blobs.Select(blob => blob.Prefix.TrimEnd('/')).ToList();
        }

        private async Task<Stream> GetFileAsync(string blobName, BlobType blobType, bool downloadParallel)
        {
            var container = _cloudBlobClient.GetContainerReference(_containerName);
            var blob = blobType switch
            {
                BlobType.AppendBlob => (CloudBlob)container.GetAppendBlobReference(blobName),
                BlobType.BlockBlob => container.GetBlockBlobReference(blobName),
                BlobType.PageBlob => container.GetPageBlobReference(blobName),
                _ => throw new ArgumentNullException($"Argument {nameof(blobType)} has incorrect value")
            };

            var blobExists = await blob.ExistsAsync().ConfigureAwait(false);

            if (blobExists)
            {
                var stream = downloadParallel
                    ? await DownloadBlobParallel(blob).ConfigureAwait(false)
                    : await DownloadBlob(blob).ConfigureAwait(false);

                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }

            return null;
        }

        private static async Task<Stream> DownloadBlob(CloudBlob blob)
        {
            var memoryStream = new MemoryStream();
            await blob.DownloadToStreamAsync(memoryStream).ConfigureAwait(false);
            return memoryStream;
        }

        private static async Task<Stream> DownloadBlobParallel(CloudBlob blob)
        {
            // tasks number
            const int batchCount = 100;

            var length = blob.Properties.Length;
            var tasksCompleted = 0;
            var batchSize = (int)Math.Ceiling((double)length / batchCount);
            var lastBatchSize = length - (batchCount - 1) * batchSize;
            var tasks = new List<Task<byte[]>>();
            var semaphore = new SemaphoreSlim(batchCount, batchCount);

            for (var i = 0; i < batchCount; i++)
            {
                var offset = i * batchSize;
                var size = i == batchCount - 1 ? lastBatchSize : batchSize;

                await semaphore.WaitAsync().ConfigureAwait(false);

                var task = Task.Run(async () =>
                {
                    var buffer = new byte[size];
                    await blob.DownloadRangeToByteArrayAsync(buffer, 0, offset, size).ContinueWith(t =>
                    {
                        semaphore.Release();
                        Interlocked.Increment(ref tasksCompleted);
                    }).ConfigureAwait(false);
                    return buffer;
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return new MemoryStream(tasks.SelectMany(t => t.Result).ToArray());
        }
    }
}
