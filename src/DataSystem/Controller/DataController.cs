using System.Net.NetworkInformation;
using DataSystem.Database;
using DataSystem.Database.Dto;
using DataSystem.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataSystem.Controller;

/// <summary>
///     API-Controller
/// </summary>
[Route("v1/data")]
[ApiController]
public class DataController : ControllerBase
{
    // map order values to linq-expressions
    private static readonly Dictionary<string, IOrderBy> OrderHelperExpressions =
        new()
        {
            { "timestamp", new OrderByHelper<SensorData, DateTime>(m => m.TimeStamp) },
            { "humidity", new OrderByHelper<SensorData, decimal?>(m => m.Humidity) },
            { "carbondioxide", new OrderByHelper<SensorData, decimal?>(m => m.CarbonDioxide) },
            { "lpg", new OrderByHelper<SensorData, decimal?>(m => m.Lpg) },
            { "temperature", new OrderByHelper<SensorData, decimal?>(m => m.Temperature) },
            { "smoke", new OrderByHelper<SensorData, decimal?>(m => m.Smoke) },
            { "light", new OrderByHelper<SensorData, bool?>(m => m.Light) },
            { "motion", new OrderByHelper<SensorData, bool?>(m => m.Motion) }
        };

    // map order operation to enum
    private static readonly Dictionary<string, OrderByEnum> OrderOperation =
        new()
        {
            { "ascending", OrderByEnum.Ascending },
            { "descending", OrderByEnum.Descending }
        };

    // ReSharper disable once InconsistentNaming
    private readonly ApplicationContext context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DataController(ApplicationContext context)
    {
        this.context = context;
    }

    /// <summary>
    ///     return entries ordered by descending timestamp
    ///     example: "GET: /api/data/latest?deviceId=B827EBBF9D51&count=100&accessToken=130e1808-40a9-46d0-9b26-80d08fe3c55b"
    /// </summary>
    /// <param name="AuthorizationToken" example="130e1808-40a9-46d0-9b26-80d08fe3c55b">the authorization token</param>
    /// <param name="DeviceId" example="B827EBBF9D51, B8:27:EB:BF:9D:51, B8-27-EB-BF-9D-51">the deviceId of the selected device</param>
    /// <param name="Count" example="100">maximum number of returned rows (max. 500). reset to 100 when out of range {0...500}</param>
    /// <returns>
    ///     dataset containing the matching rows when deviceId is specified, otherwise dataset containing rows ordered by
    ///     descending timestamp. it contains a maximum number of rows specified by the count parameter
    /// </returns>
    /// <response code="200">Data returned</response>
    /// <response code="204">The request yielded no data</response>
    /// <response code="400">Not authorized</response>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(List<SensorDataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<SensorDataDto>>> GetLatestData(string AuthorizationToken, string? DeviceId = null,
        int Count = 100)
    {
        // reset to 100 entries when out of range
        if (!Enumerable.Range(1, 500).Contains(Count))
            Count = 100;

        // check for existing Authorization
        var authorizationEntry =
            await Authorization.CheckForAuthorizationFlags(context, AuthorizationToken, AuthorizeFlags.Read, false);

        // return if token is not allowed or locked
        if (authorizationEntry == null)
            return BadRequest();

        // get generic queryable when deviceId is not set
        var queryable = DeviceId != null && PhysicalAddress.TryParse(DeviceId, out var parsedDeviceId)
            ? context.SensorData.Where(i => i.DeviceId.Equals(parsedDeviceId))
            : context.SensorData.AsQueryable();

        var result = queryable.OrderByDescending(i => i.TimeStamp).Take(Count).Select(i => new SensorDataDto(i));

        if (result.Any())
            return await result.ToListAsync();

        return NoContent();
    }

    /// <summary>
    ///     return entries based on provided ordering and on which values should be ordered
    ///     example: "GET: /api/data?accessToken=130e1808-40a9-46d0-9b26-80d08fe3c55b&deviceId=B827EBBF9D51&pageIndex=1
    ///     &pageSize=25&order=ascending&orderValue=temperature"
    /// </summary>
    /// <param name="AuthorizationToken" example="130e1808-40a9-46d0-9b26-80d08fe3c55b">the authorization token</param>
    /// <param name="DeviceId" example="B827EBBF9D51, B8:27:EB:BF:9D:51, B8-27-EB-BF-9D-51">the deviceId of the selected device</param>
    /// <param name="PageIndex" example="1">the current page when pagination is used</param>
    /// <param name="PageSize" example="25">the count of items per page</param>
    /// <param name="Order" example="ascending, descending">ascending or descending order</param>
    /// <param name="OrderValue" example="timestamp, humidity, carbondioxide, lpg, temperature, smoke, light, motion">
    ///     on which
    ///     value should be ordered
    /// </param>
    /// <returns>
    ///     a paginated dataset
    /// </returns>
    /// <response code="200">Data returned</response>
    /// <response code="204">The request yielded no data</response>
    /// <response code="400">Not authorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<SensorDataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<SensorDataDto>>> GetData(string AuthorizationToken, string? DeviceId = null,
        int PageIndex = 0, int PageSize = 25, string Order = "descending",
        string OrderValue = "timestamp")
    {
        // reset to 100 entries when out of range
        if (!Enumerable.Range(1, 100).Contains(PageSize))
            PageSize = 25;

        // check for existing Authorization
        var authorizationEntry =
            await Authorization.CheckForAuthorizationFlags(context, AuthorizationToken, AuthorizeFlags.Read, false);

        // return if token is not allowed or locked
        if (authorizationEntry == null)
            return BadRequest();

        // get generic queryable when deviceId is not set
        var query = DeviceId != null && PhysicalAddress.TryParse(DeviceId, out var parsedDeviceId)
            ? context.SensorData.Where(i => i.DeviceId.Equals(parsedDeviceId))
            : context.SensorData.AsQueryable();

        // resets to "timestamp" when wrong value is provided
        if (!OrderHelperExpressions.ContainsKey(OrderValue))
            OrderValue = OrderHelperExpressions.First().Key;

        // resets to "descending" when wrong value is provided
        if (!OrderOperation.ContainsKey(Order))
            Order = OrderOperation.Last().Key;

        query = OrderOperation[Order] == OrderByEnum.Descending
            ? query.OrderByDescending(OrderHelperExpressions[OrderValue])
            : query.OrderBy(OrderHelperExpressions[OrderValue]);

        // get full entry count
        var count = await query.CountAsync();

        // get actual paginated data
        var results = query.Skip(PageIndex * PageSize).Take(PageSize).Select(i => new SensorDataDto(i));

        // check if results is not empty
        if (results.Any())
            return new PaginatedResult<SensorDataDto>(await results.ToListAsync(), PageIndex,
                (int)Math.Ceiling(count / (double)PageSize));

        return NoContent();
    }
}

// paginated result with indices and total count/pages
public class PaginatedResult<T>(List<T> items, int pageIndex, int totalPages)
{
    // ReSharper disable once MemberCanBePrivate.Global
    public int PageIndex { get; } = pageIndex;

    // ReSharper disable once MemberCanBePrivate.Global
    public int TotalPages { get; } = totalPages;
    public List<T> Items { get; } = items;

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

// order enum
public enum OrderByEnum
{
    Ascending,
    Descending
}