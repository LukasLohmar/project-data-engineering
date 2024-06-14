using System.Globalization;
using System.Net.NetworkInformation;
using DataSystem.Database;
using DataSystem.Grpc;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace DataSystem.Service;

/// <summary>
/// gRPC-Controller
/// </summary>
public class DataService : Grpc.DataService.DataServiceBase
{
    // ReSharper disable once InconsistentNaming
    private readonly ApplicationContext context;
    
    // ReSharper disable once ConvertToPrimaryConstructor
    public DataService(ApplicationContext context) => this.context = context;

    /// <summary>
    /// save sensor-data to the data-system
    /// </summary>
    /// <param name="request">the request data</param>
    /// <param name="serverContext">server-side call context. clients wont need to provide this parameter</param>
    /// <returns>return BasicReply which consists of a ResponseState and a message when the call fails</returns>
    public override async Task<BasicReply> Save(SaveRequest request, ServerCallContext serverContext)
    {
        // check for token
        if (string.IsNullOrEmpty(request.AuthorizationToken) || ! Guid.TryParse(request.AuthorizationToken.AsSpan(), out var guid))
            return await Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseUnauthorized));
        
        // check if token exist and allows for writing data
        var authorizationEntry =
            await context.Authorization.FirstOrDefaultAsync(i =>
                i.Token == guid && i.Locked == false &&i.AuthorizedFlags.HasFlag(AuthorizeFlags.Write));
        
        if (authorizationEntry == null)
            return await Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseUnauthorized));
        
        // check for device id -> do not save if mac is not provided
        if (string.IsNullOrEmpty(request.DeviceId) || !PhysicalAddress.TryParse(request.DeviceId, out var deviceId))
            return await Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseInternalServerError, "no device-id was provided"));
        
        // check for timestamp
        if (string.IsNullOrEmpty(request.TimeStamp) || !DateTime.TryParse(request.TimeStamp, out var timeStampValue))
            return await Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseInternalServerError, "no timestamp was provided"));

        var newEntry = new SensorData
        {
            TimeStamp = timeStampValue,
            DeviceId  = deviceId,
            CarbonDioxide = request.CarbonDioxide.HasValue ? (decimal) request.CarbonDioxide.Value : null,
            Humidity = request.Humidity.HasValue ? (decimal) request.Humidity.Value : null,
            Light = request.Light,
            Lpg = request.Lpg.HasValue ? (decimal) request.Lpg.Value : null,
            Motion = request.Motion,
            Smoke = request.Smoke.HasValue ? (decimal) request.Smoke.Value : null,
            Temperature = request.Temperature.HasValue ? (decimal) request.Temperature.Value : null,
            AdditionalData = request.AdditionalData,
            // link token to the created data
            ProviderToken = authorizationEntry
        };

        await context.SensorData.AddAsync(newEntry);
        await context.SaveChangesAsync();

        return await Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseOk));
    }

    private static BasicReply CreateResult(BasicReply.Types.ResponseValue responseValue, string? errorMessage = null) {
        return new BasicReply
        {
            ResponseState = responseValue,
            ResponseMessage = responseValue switch
            {
                BasicReply.Types.ResponseValue.ResponseOk => "values saved to system",
                BasicReply.Types.ResponseValue.ResponseUnauthorized => "unauthorized request",
                BasicReply.Types.ResponseValue.ResponseInternalServerError => $"error: {errorMessage}",
                _ => "",
            }
        };
    }
}
