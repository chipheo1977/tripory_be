using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Social.Commands;

public record DeleteCommentCommand(Guid ItineraryId, Guid CommentId) : ICommand;
