using System;
using System.Globalization;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sigma.IoT.API.Models;
using Sigma.IoT.Data;

namespace Sigma.IoT.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion(ApiVersions.V1)]
    public class DevicesController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public DevicesController(ICacheService cacheService, IMapper mapper)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Returns data from selected device, date and sensor
        /// </summary>
        /// <param name="deviceName" example="dockan"/>
        /// <param name="date" example="2019-01-13">Date in format yyyy-MM-dd</param>
        /// <param name="sensorType" example="humidity">Possible values: humidity, rainfall, temperature</param>
        [HttpGet]
        [MapToApiVersion(ApiVersions.V1)]
        [Route("{deviceName}/data/{date}/{sensorType}")]
        [ProducesResponseType(typeof(GetDataForSensorResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDataAsync(string deviceName, string date, string sensorType)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw new ArgumentException($"Argument {nameof(deviceName)} cannot be null or empty string");
            }

            var data = await _cacheService.GetDataAsync(deviceName, ParseDate(date), ParseSensorType(sensorType));
            var responseModel = _mapper.Map<GetDataForSensorResponseModel>(data, x => x.AfterMap((src, dest) =>
            {
                dest.Device = deviceName; 
                dest.Date = date;
                dest.Sensor = sensorType;

            }));
            return Ok(responseModel);
        }

        /// <summary>
        /// Returns data from selected device and date
        /// </summary>
        /// <param name="deviceName" example="dockan"/>
        /// <param name="date" example="2019-01-13">Date in format yyyy-MM-dd</param>
        [HttpGet]
        [MapToApiVersion(ApiVersions.V1)]
        [Route("{deviceName}/data/{date}")]
        [ProducesResponseType(typeof(GetDataForAllSensorsResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDataAsync(string deviceName, string date)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw new ArgumentException($"Argument {nameof(deviceName)} cannot be null or empty string");
            }

            var data = await _cacheService.GetDataAsync(deviceName, ParseDate(date));
            var responseModel = _mapper.Map<GetDataForAllSensorsResponseModel>(data, x => x.AfterMap((src, dest) =>
            {
                dest.Device = deviceName;
                dest.Date = date;
            }));
            return Ok(responseModel);
        }

        private static DateTime ParseDate(string date) =>
            DateTime.TryParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate)
                    ? parsedDate
                    : throw new ArgumentException($"Argument {nameof(date)} has incorrect format");

        private static SensorType ParseSensorType(string sensorType) =>
            sensorType.ToLowerInvariant() switch
            {
                "humidity" => SensorType.Humidity,
                "rainfall" => SensorType.Rainfall,
                "temperature" => SensorType.Temperature,
                _ => throw new ArgumentException($"Argument {nameof(sensorType)} is incorrect")
            };
    }
}
