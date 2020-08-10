using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sigma.IoT.API.Controllers;
using Sigma.IoT.API.Models;
using Sigma.IoT.Data;
using Sigma.IoT.Tests.Shared;
using Xunit;

namespace Sigma.IoT.Tests.Controllers
{
    public class DevicesControllerTests
    {
        [Fact]
        public void Controller_is_not_created_when_cache_service_is_not_passed() =>
            Assert.Throws<ArgumentNullException>(() => new DevicesController(null, DefaultMocks.Mapper));


        [Fact]
        public void Controller_is_not_created_when_mapper_is_not_passed() =>
            Assert.Throws<ArgumentNullException>(() => new DevicesController(DefaultMocks.CacheService, null));

        [Fact]
        public async Task Controller_returns_correct_data()
        {
            var cacheServiceMock = new Mock<ICacheService>();
            var mapperMock = new Mock<IMapper>();

            cacheServiceMock.Setup(
                    x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<DateTime>(), null))
                .Returns(() => Task.FromResult<IReadOnlyCollection<SensorData>>(new List<SensorData>
                {
                    new SensorData(new DateTime(2019, 1, 1, 1, 25,0), 10, SensorType.Humidity ),
                    new SensorData(new DateTime(2020, 2, 2, 2, 30,0), 20, SensorType.Rainfall ),
                    new SensorData(new DateTime(2021, 3, 3, 3, 35,0), 30, SensorType.Temperature )
                }));

            mapperMock.Setup(x => x.Map(It.IsAny<object>(),
                    It.IsAny<Action<IMappingOperationOptions<object, GetDataForAllSensorsResponseModel>>>()))
                .Returns(new GetDataForAllSensorsResponseModel
                {
                    Date = "2019-01-01",
                    Device = "testDevice",
                    SensorData = new Dictionary<string, IEnumerable<UnitDataModel>>
                    {
                        { "humidity", new List<UnitDataModel> { new UnitDataModel { Value = 10, Time = "01:25:00" } } },
                        { "rainfall", new List<UnitDataModel> { new UnitDataModel { Value = 20, Time = "01:30:00" } } },
                        { "temperature", new List<UnitDataModel> { new UnitDataModel { Value = 30, Time = "01:35:00" } } }
                    }
                });

            var response = await new DevicesController(cacheServiceMock.Object, mapperMock.Object)
                .GetDataAsync("testDevice", "2019-01-01");

            var okResponse = Assert.IsType<OkObjectResult>(response);
            var responseValue = Assert.IsType<GetDataForAllSensorsResponseModel>(okResponse.Value);

            var expectedData = new Dictionary<string, IEnumerable<UnitDataModel>>
            {
                {"humidity", new List<UnitDataModel> {new UnitDataModel {Value = 10, Time = "01:25:00"}}},
                {"rainfall", new List<UnitDataModel> {new UnitDataModel {Value = 20, Time = "01:30:00"}}},
                {"temperature", new List<UnitDataModel> {new UnitDataModel {Value = 30, Time = "01:35:00"}}}
            };

            cacheServiceMock.Verify(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<DateTime>(), null), Times.Once);
            mapperMock.Verify(x =>
                x.Map(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions<object, GetDataForAllSensorsResponseModel>>>()),
                Times.Once);

            Assert.Equal("2019-01-01", responseValue.Date);
            Assert.Equal("testDevice", responseValue.Device);
            Assert.True(IsEqual(expectedData, responseValue.SensorData));
        }

        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [Theory]
        public async Task Controller_throws_exception_when_incorrect_device_name_is_passed(string deviceName) =>
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await new DevicesController(DefaultMocks.CacheService, DefaultMocks.Mapper).GetDataAsync(deviceName, "2019-01-01"));

        [InlineData("")]
        [InlineData(" ")]
        [InlineData("wrong date")]
        [InlineData("01-01-2020")]
        [InlineData(null)]
        [Theory]
        public async Task Controller_throws_exception_when_incorrect_date_is_passed(string date) =>
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await new DevicesController(DefaultMocks.CacheService, DefaultMocks.Mapper).GetDataAsync("Test Device", date));

        private bool IsEqual(
            IDictionary<string, IEnumerable<UnitDataModel>> dictionaryA,
            IDictionary<string, IEnumerable<UnitDataModel>> dictionaryB)
        {
            if (dictionaryA == null && dictionaryB == null)
            {
                return true;
            }

            var equal = dictionaryA != null && dictionaryB != null &&
                        dictionaryA.Count == dictionaryB.Count &&
                        dictionaryA.Keys.All(dictionaryB.Keys.Contains);

            if (!equal)
            {
                return equal;
            }

            foreach (var key in dictionaryA.Keys)
            {
                var collection1 = dictionaryA[key];
                var collection2 = dictionaryB[key];

                if (collection1.Count() == collection2.Count())
                {
                    equal &= collection1.All(e1 => collection2.FirstOrDefault(e2 => e1.Value == e2.Value && e1.Time == e2.Time) != null);
                }
            }

            return equal;
        }
    }
}
