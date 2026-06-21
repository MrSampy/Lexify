using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.ToggleLanguage;

public sealed record ToggleLanguageCommand(string Code) : IRequest<Result>;
