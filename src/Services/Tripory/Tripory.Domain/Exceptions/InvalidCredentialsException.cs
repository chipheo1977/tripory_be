using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class InvalidCredentialsException : BadRequestException
{
    public InvalidCredentialsException()
        : base("Thông tin đăng nhập (Email hoặc Mật khẩu) không chính xác.")
    {
    }
}
