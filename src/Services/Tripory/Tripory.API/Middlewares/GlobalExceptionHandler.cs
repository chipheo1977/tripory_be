using BuildingBlocks.Core.Domains.Abstractions.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Tripory.API.Common.Responses;

namespace Tripory.API.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Đã xảy ra lỗi không xử lý được: {Message}", exception.Message);

        var (statusCode, errorCode, message) = exception switch
        {
            NotFoundException nf => (StatusCodes.Status404NotFound, "NotFound", nf.Message),
            ConflictException cf => (StatusCodes.Status409Conflict, "Conflict", cf.Message),
            BadRequestException br => (StatusCodes.Status400BadRequest, "BadRequest", br.Message),
            DomainException de => (StatusCodes.Status400BadRequest, de.Title, de.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server.Error", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse.Failure(errorCode, message);
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
