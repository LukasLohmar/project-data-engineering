using System.Net.NetworkInformation;
using DataSystem.Database;
using DataSystem.Extensions;
using DataSystem.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace DataSystem.Service;

/// <summary>
///     gRPC-Controller
/// </summary>
public class DataService : Grpc.DataService.DataServiceBase
{
    // default values for requests
    // ReSharper disable once InconsistentNaming
    private const int DEFAULT_PAGE_INDEX = 1;

    // ReSharper disable once InconsistentNaming
    private const int DEFAULT_PAGE_SIZE = 100;

    // ReSharper disable once InconsistentNaming
    private const int MIN_PAGE_SIZE = 1;

    // ReSharper disable once InconsistentNaming
    private const int MAX_PAGE_SIZE = 500;

    private static readonly Dictionary<RequestOrderValue, IOrderBy> OrderHelperExpressions =
        new()
        {
            { RequestOrderValue.OrderValueDefault, new OrderByHelper<SensorData, int>(m => m.Id) },
            { RequestOrderValue.OrderValueByTimestamp, new OrderByHelper<SensorData, DateTime>(m => m.TimeStamp) },
            { RequestOrderValue.OrderValueByHumidity, new OrderByHelper<SensorData, decimal?>(m => m.Humidity) },
            {
                RequestOrderValue.OrderValueByCarbonDioxide,
                new OrderByHelper<SensorData, decimal?>(m => m.CarbonDioxide)
            },
            { RequestOrderValue.OrderValueByLpg, new OrderByHelper<SensorData, decimal?>(m => m.Lpg) },
            { RequestOrderValue.OrderValueByTemperature, new OrderByHelper<SensorData, decimal?>(m => m.Temperature) },
            { RequestOrderValue.OrderValueBySmoke, new OrderByHelper<SensorData, decimal?>(m => m.Smoke) },
            { RequestOrderValue.OrderValueByLight, new OrderByHelper<SensorData, bool?>(m => m.Light) },
            { RequestOrderValue.OrderValueByMotion, new OrderByHelper<SensorData, bool?>(m => m.Motion) }
        };

    // ReSharper disable once InconsistentNaming
    private readonly ApplicationContext Context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DataService(ApplicationContext context)
    {
        Context = context;
    }

    public override async Task<BasicPostReply> Save(SaveRequest request, ServerCallContext context)
    {
        // check if token exist and allows for writing data
        var authorizationEntry =
            await Authorization.CheckForAuthorizationFlags(Context, request.AuthorizationToken, AuthorizeFlags.Write,
                false);

        if (authorizationEntry == null)
            return await Task.FromResult(CreatePostResult(RequestResponseType.ResponseUnauthorized));

        if (request.TimeStamp is null)
            return await Task.FromResult(CreatePostResult(RequestResponseType.ResponseInternalError,
                "no timestamp was provided"));

        // check for device id -> do not save if mac is not provided
        if (string.IsNullOrEmpty(request.DeviceId) || !PhysicalAddress.TryParse(request.DeviceId, out var deviceId))
            return await Task.FromResult(CreatePostResult(RequestResponseType.ResponseInternalError,
                "no device-id was provided"));

        var newEntry = new SensorData
        {
            // remove timezone
            TimeStamp = DateTimeOffset.Parse(request.TimeStamp.ToDateTime().ToLongDateString(), null).DateTime,
            DeviceId = deviceId,
            CarbonDioxide = request.CarbonDioxide.HasValue ? (decimal)request.CarbonDioxide.Value : null,
            Humidity = request.Humidity.HasValue ? (decimal)request.Humidity.Value : null,
            Light = request.Light,
            Lpg = request.Lpg.HasValue ? (decimal)request.Lpg.Value : null,
            Motion = request.Motion,
            Smoke = request.Smoke.HasValue ? (decimal)request.Smoke.Value : null,
            Temperature = request.Temperature.HasValue ? (decimal)request.Temperature.Value : null,
            AdditionalData = request.AdditionalData,
            // link token to the created data
            ProviderToken = authorizationEntry
        };

        await Context.SensorData.AddAsync(newEntry);
        await Context.SaveChangesAsync();

        return await Task.FromResult(CreatePostResult(RequestResponseType.ResponseOk));
    }

    public override async Task<BasicPagedReply> GetData(GetDataRequest request, ServerCallContext context)
    {
        var pageSize = request.PageSize.GetValueOrDefault(DEFAULT_PAGE_SIZE);
        var pageIndex = request.PageIndex.GetValueOrDefault(DEFAULT_PAGE_INDEX);

        if (pageIndex < DEFAULT_PAGE_INDEX)
            pageIndex = DEFAULT_PAGE_INDEX;

        // reset to 100 entries when out of range
        if (!Enumerable.Range(MIN_PAGE_SIZE, MAX_PAGE_SIZE).Contains(pageSize))
            pageSize = DEFAULT_PAGE_SIZE;

        // check for existing Authorization
        var authorizationEntry =
            await Authorization.CheckForAuthorizationFlags(Context, request.AuthorizationToken, AuthorizeFlags.Read,
                false);

        // return if token is not allowed or locked
        if (authorizationEntry == null)
            return CreatePagedReply(RequestResponseType.ResponseUnauthorized, new List<SensorDataDto>(), 0, 0);

        // get generic queryable when deviceId is not set
        var query = request.DeviceId != null && PhysicalAddress.TryParse(request.DeviceId, out var parsedDeviceId)
            ? Context.SensorData.Where(i => i.DeviceId.Equals(parsedDeviceId))
            : Context.SensorData.AsQueryable();

        query = request.Order is RequestOrderBy.OrderByDescending or RequestOrderBy.OrderByDefault
            ? query.OrderByDescending(OrderHelperExpressions[request.OrderValue])
            : query.OrderBy(OrderHelperExpressions[request.OrderValue]);

        var entryDate = request.EntryDate is null
            ? DateTime.UnixEpoch
            : request.EntryDate.DateCase switch
            {
                NullableTimeStamp.DateOneofCase.None => DateTime.UnixEpoch,
                NullableTimeStamp.DateOneofCase.Null => DateTime.UnixEpoch,
                NullableTimeStamp.DateOneofCase.Value => request.EntryDate.Value.ToDateTime(),
                _ => DateTime.UnixEpoch
            };

        if (!entryDate.Equals(DateTime.UnixEpoch))
        {
            // to short date work around - remove timezone from datetime object
            var startDate = DateTimeOffset.Parse(entryDate.Date.ToShortDateString(), null);
            var endDate = startDate.AddDays(1);

            query = query.Where(i => i.TimeStamp > startDate && i.TimeStamp < endDate);
        }

        // get full entry count
        var count = await query.CountAsync();

        // get actual paginated data
        var results = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).Select(i => new SensorDataDto
        {
            Id = i.Id,
            TimeStamp = Timestamp.FromDateTime(i.TimeStamp.ToUniversalTime()),
            DeviceId = i.DeviceId.ToString(),
            CarbonDioxide = i.CarbonDioxide.HasValue ? (double)i.CarbonDioxide.Value : null,
            Humidity = i.Humidity.HasValue ? (double)i.Humidity.Value : null,
            Light = i.Light,
            Lpg = i.Lpg.HasValue ? (double)i.Lpg.Value : null,
            Motion = i.Motion,
            Smoke = i.Smoke.HasValue ? (double)i.Smoke.Value : null,
            Temperature = i.Temperature.HasValue ? (double)i.Temperature.Value : null,
            AdditionalData = i.AdditionalData
        });

        // check if results is not empty
        if (results.Any())
            return CreatePagedReply(RequestResponseType.ResponseOk, await results.ToListAsync(), pageIndex,
                (int)Math.Ceiling(count / (double)pageSize));

        return CreatePagedReply(RequestResponseType.ResponseNoContent, new List<SensorDataDto>(), pageIndex,
            (int)Math.Ceiling(count / (double)pageSize));
    }

    private static BasicPostReply CreatePostResult(RequestResponseType responseValue, string? errorMessage = null)
    {
        return new BasicPostReply
        {
            ResponseState = responseValue,
            ResponseMessage = responseValue switch
            {
                RequestResponseType.ResponseOk => "values saved to system",
                RequestResponseType.ResponseUnauthorized => "unauthorized request",
                RequestResponseType.ResponseInternalError => $"error: {errorMessage}",
                RequestResponseType.ResponseDefault => "",
                _ => ""
            }
        };
    }

    private static BasicPagedReply CreatePagedReply(RequestResponseType responseType, List<SensorDataDto>? items,
        int pageIndex, int totalPages)
    {
        var result = new BasicPagedReply
        {
            ResponseState = responseType,
            PageIndex = pageIndex,
            TotalPages = totalPages,
            HasNextPage = pageIndex < totalPages,
            HasPreviousPage = pageIndex > 1
        };

        result.Items.Add(items);

        return result;
    }
}