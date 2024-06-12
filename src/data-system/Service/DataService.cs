using DataSystem.Grpc;
using Grpc.Core;

namespace DataSystem.Service;

public class DataService : Grpc.DataService.DataServiceBase
{
    // rpc entrypoint where data will be collected
    public override Task<BasicReply> Save(SaveRequest request, ServerCallContext context)
    {
        // check for timestamp
        if (request.AuthorizationKey?.Equals(string.Empty) == null)
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
