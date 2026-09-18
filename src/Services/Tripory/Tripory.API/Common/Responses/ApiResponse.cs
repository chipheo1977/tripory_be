namespace Tripory.API.Common.Responses;

public class ApiResponse<T>
{
    public string Status { get; init; } = "success";
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Success(T data, string message = "Thành công") =>
        new()
        {
            Status = "success",
            Data = data,
            Message = message
        };

    public static ApiResponse<T> Failure(string errorCode, string message) =>
        new()
        {
            Status = "error",
            Data = default,
            ErrorCode = errorCode,
            Message = message
        };
}

public static class ApiResponse
{
    public static ApiResponse<object> Success(string message = "Thành công") =>
        ApiResponse<object>.Success(new { }, message);

    public static ApiResponse<object> Failure(string errorCode, string message) =>
        ApiResponse<object>.Failure(errorCode, message);
}
