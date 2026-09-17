// File: src/Services/Tripory/Tripory.Domain/Exceptions/RefreshTokenException.cs
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class RefreshTokenException : BadRequestException
{
    public RefreshTokenException(string message)
        : base(message)
    {
    }
}
