using BuildingBlocks.Core.Abstractions.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tripory.API.Common.Responses;

namespace Tripory.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiController : ControllerBase
{
    protected ISender Sender { get; }

    protected ApiController(ISender sender)
    {
        Sender = sender;
    }

    protected IActionResult HandlerFailure(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Không thể xử lý lỗi trên một Result thành công.");

        var response = ApiResponse.Failure(result.Error.Code, result.Error.Message);

        return result.Error.Code switch
        {
            var c when c.Contains("NotFound") => NotFound(response),
            var c when c.Contains("Conflict") || c.Contains("AlreadyExists") => Conflict(response),
            var c when c.Contains("Unauthorized") => Unauthorized(response),
            var c when c.Contains("Forbidden") => StatusCode(StatusCodes.Status403Forbidden, response),
            _ => BadRequest(response)
        };
    }
}
