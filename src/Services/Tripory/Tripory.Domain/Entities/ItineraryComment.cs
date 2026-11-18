using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using Tripory.Domain.Errors;

namespace Tripory.Domain.Entities;

public class ItineraryComment : EntityBase<Guid>, IAggregateRoot
{
    public const int ContentMaxLength = 500;

    public Guid ItineraryId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private ItineraryComment()
    {
    }

    private ItineraryComment(
        Guid id,
        Guid itineraryId,
        Guid userId,
        string content,
        DateTimeOffset createdAt
    )
    {
        Id = id;
        ItineraryId = itineraryId;
        UserId = userId;
        Content = content;
        CreatedAt = createdAt;
    }

    public static Result<ItineraryComment> Create(Itinerary itinerary, Guid userId, string? content)
    {
        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return Result.Failure<ItineraryComment>(socialResult.Error);

        if (userId == Guid.Empty)
            return Result.Failure<ItineraryComment>(DomainErrors.Itinerary.InvalidUserId);

        var trimmed = content?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return Result.Failure<ItineraryComment>(DomainErrors.Comment.ContentRequired);

        if (trimmed.Length > ContentMaxLength)
            return Result.Failure<ItineraryComment>(DomainErrors.Comment.ContentTooLong);

        var comment = new ItineraryComment(
            Guid.NewGuid(),
            itinerary.Id,
            userId,
            trimmed,
            DateTimeOffset.UtcNow
        );

        return Result.Success(comment);
    }

    public Result EnsureCanBeDeletedBy(Itinerary itinerary, Guid userId)
    {
        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return socialResult;

        if (ItineraryId != itinerary.Id)
            return Result.Failure(DomainErrors.Comment.NotFound);

        if (userId == Guid.Empty || (userId != UserId && userId != itinerary.UserId))
            return Result.Failure(DomainErrors.Comment.DeleteForbidden);

        return Result.Success();
    }
}
