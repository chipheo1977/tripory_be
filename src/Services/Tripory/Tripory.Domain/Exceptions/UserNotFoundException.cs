using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(Guid userId) 
        : base($"Không tìm thấy người dùng với định danh: {userId}")
    {
        
    }

    public UserNotFoundException(string identifier) 
        : base($"Không tìm thấy người dùng với thông tin: {identifier}")
    {
        
    }
}
