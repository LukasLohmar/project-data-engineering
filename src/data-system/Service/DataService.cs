using DataSystem.Grpc;
using Grpc.Core;

namespace DataSystem.Service;

/// <summary>
/// gRPC-Controller
/// </summary>
public class DataService : Grpc.DataService.DataServiceBase
{
    /// <summary>
    /// save sensor-data to the data-system
    /// </summary>
    /// <param name="request">the request data</param>
    /// <param name="context">server-side call context. clients wont need to provide this parameter</param>
    /// <returns>return BasicReply which consists of a ResponseState and a message when the call fails</returns>
    public override Task<BasicReply> Save(SaveRequest request, ServerCallContext context)
    {
        // check for timestamp
        if (request.AuthorizationToken?.Equals(string.Empty) == null)
            return Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseUnauthorized));
        
        // check for device id -> dont save if mac is not provided
        if (request.DeviceId?.Equals(string.Empty) == null)
            return Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseInternalServerError, "no device-id was provided"));
        
        // check for timestamp
        if (request.TimeStamp?.Equals(string.Empty) == null)
            return Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseInternalServerError, "no timestamp was provided"));


        return Task.FromResult(CreateResult(BasicReply.Types.ResponseValue.ResponseOk));
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
