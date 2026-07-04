using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.UserProfile.Commands.UpdateEnglishLevel;

/// <param name="EnglishLevel">CEFR level (A1..C2) or null to clear.</param>
public sealed record UpdateEnglishLevelCommand(Guid UserId, string? EnglishLevel) : IRequest<Result>;
