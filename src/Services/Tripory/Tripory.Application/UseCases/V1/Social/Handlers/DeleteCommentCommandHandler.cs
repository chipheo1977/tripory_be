using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Social.Commands;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Social.Handlers;

public class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryCommentRepository _itineraryCommentRepository;

    public DeleteCommentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository,
        IItineraryCommentRepository itineraryCommentRepository)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
        _itineraryCommentRepository = itineraryCommentRepository;
    }

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is not Guid userId)
            return Result.Failure(DomainErrors.Auth.Unauthorized);

        var itinerary = await _itineraryRepository.FindByIdAsync(request.ItineraryId, ct);
        if (itinerary is null)
            return Result.Failure(DomainErrors.Itinerary.NotFound);

        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return socialResult;

        var comment = await _itineraryCommentRepository.FindByIdAsync(request.CommentId, ct);
        if (comment is null)
            return Result.Failure(DomainErrors.Comment.NotFound);

        var deleteResult = comment.EnsureCanBeDeletedBy(itinerary, userId);
        if (deleteResult.IsFailure)
            return deleteResult;

        await _itineraryCommentRepository.RemoveAsync(comment);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
