using Lexify.Application.Common;
using Lexify.Application.Conversations.Dtos;
using MediatR;

namespace Lexify.Application.Conversations.Commands.StartConversation;

/// <param name="BlockId">Optional scope — practise words from this block; null = words due across all blocks.</param>
/// <param name="Scenario">Optional roleplay framing (e.g. "ordering at a café"); null = free chat.</param>
/// <param name="NativeLanguage">The learner's language (UI locale), used only to frame gentle corrections.</param>
public sealed record StartConversationCommand(
    Guid UserId,
    Guid? BlockId,
    string? Scenario,
    string NativeLanguage)
    : IRequest<Result<StartConversationResultDto>>;
