using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public class ConversationNotFoundException : NotFoundException
{
    public ConversationNotFoundException(Guid id) 
        : base($"Không tìm thấy cuộc gọi với mã: {id}")
    {
    }
}