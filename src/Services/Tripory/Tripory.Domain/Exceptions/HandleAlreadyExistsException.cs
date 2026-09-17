using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class HandleAlreadyExistsException : ConflictException
{
    public HandleAlreadyExistsException(string handle)
        : base($"Handle '{handle}' đã tồn tại trong hệ thống.")
    {
    }
}
