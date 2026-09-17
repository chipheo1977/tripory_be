using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class EmailAlreadyExistsException : ConflictException
{
    public EmailAlreadyExistsException(string email)
        : base($"Email '{email}' đã được sử dụng bởi một tài khoản khác.")
    {
    }
}
