using FluentValidation;

namespace Lexify.Application.Conversations.Commands.StartConversation;

public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NativeLanguage).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Scenario).MaximumLength(200);
    }
}
