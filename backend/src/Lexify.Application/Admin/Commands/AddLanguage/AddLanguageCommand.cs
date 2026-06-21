using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Commands.AddLanguage;

public sealed record AddLanguageCommand(
    string Code,
    string Name,
    string NativeName,
    short SortOrder = 0) : IRequest<Result<LanguageDto>>;
