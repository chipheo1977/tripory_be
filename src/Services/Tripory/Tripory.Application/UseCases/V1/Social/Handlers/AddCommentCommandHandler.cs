using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Social.Commands;
using Tripory.Application.UseCases.V1.Social.Extensions;
using Tripory.Application.UseCases.V1.Social.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Social.Handlers;

public class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, CommentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryCommentRepository _itineraryCommentRepository;
    private readonly IUserRepository _userRepository;

    public AddCommentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository,
        IItineraryCommentRepository itineraryCommentRepository,
        IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
        _itineraryCommentRepository = itineraryCommentRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CommentDto>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is not Guid userId)
            return Result.Failure<CommentDto>(DomainErrors.Auth.Unauthorized);

        var itinerary = await _itineraryRepository.FindByIdAsync(request.ItineraryId, ct);
        if (itinerary is null)
            return Result.Failure<CommentDto>(DomainErrors.Itinerary.NotFound);

        var commentResult = ItineraryComment.Create(itinerary, userId, request.Content);
        if (commentResult.IsFailure)
            return Result.Failure<CommentDto>(commentResult.Error);

        var comment = commentResult.Value;
        await _itineraryCommentRepository.AddAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var authors = await _userRepository.GetSummariesByIdsAsync([userId], ct);

        return Result.Success(comment.ToDto(authors.FirstOrDefault()));
    }
}
