using System.IO;

namespace Sigma.IoT.Data
{
    public interface IFileToDataConverter<in TSource, out TDestination>
        where TSource : Stream
    {
        string GetFileExtension();

        TDestination Convert(TSource file);
    }
}
