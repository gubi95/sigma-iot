using AutoMapper;
using Moq;
using Sigma.IoT.Data;

namespace Sigma.IoT.Tests.Shared
{
    internal sealed class DefaultMocks
    {
        public static readonly IMapper Mapper = new Mock<IMapper>().Object;
        public static readonly ICacheService CacheService = new Mock<ICacheService>().Object;
        public static readonly IFilePathBuilder FilePathBuilder = new TestFilePathBuilder();
    }
}
