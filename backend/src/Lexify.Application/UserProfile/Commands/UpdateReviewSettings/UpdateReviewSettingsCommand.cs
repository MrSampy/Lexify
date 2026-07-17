using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateReviewSettings;

/// <param name="NewWordsPerDay">Max never-reviewed words introduced into the review queue per UTC day (0–100).</param>
public sealed record UpdateReviewSettingsCommand(Guid UserId, int NewWordsPerDay) : IRequest<Result>;
