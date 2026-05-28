using Microsoft.AspNetCore.Mvc;
using SpaceXLaunches.Domain.Common;

namespace SpaceXLaunches.API.Extension
{
    public static class ResponseExtensions
    {
        public static ActionResult ToHttpResponse<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return new OkObjectResult(result.Value);
            }

            return result.Error.Code switch
            {
                ErrorCodes.SpaceXApiNotFound => new NotFoundObjectResult(new ErrorResponse(result.Error)),
                ErrorCodes.InvalidLaunchId => new BadRequestObjectResult(new ErrorResponse(result.Error)),
                ErrorCodes.SpaceXApiUnavailable => new ObjectResult(new ErrorResponse(result.Error)) { StatusCode = 502 },
                ErrorCodes.SpaceXApiParseFailure => new ObjectResult(new ErrorResponse(result.Error)) { StatusCode = 502 },
                ErrorCodes.DatabaseReadFailure => new ObjectResult(new ErrorResponse(result.Error)) { StatusCode = 503 },
                ErrorCodes.DatabaseWriteFailure => new ObjectResult(new ErrorResponse(result.Error)) { StatusCode = 503 },
                _ => new ObjectResult(new ErrorResponse(result.Error)) { StatusCode = 500 }
            };
        }

        public record ErrorResponse(string Code, string Message)
        {
            public ErrorResponse(Error error) : this(error.Code, error.Message) { }
        }
    }
}