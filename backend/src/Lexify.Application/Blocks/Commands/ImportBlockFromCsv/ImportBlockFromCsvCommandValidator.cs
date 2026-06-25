using FluentValidation;

namespace Lexify.Application.Blocks.Commands.ImportBlockFromCsv;

public sealed class ImportBlockFromCsvCommandValidator : AbstractValidator<ImportBlockFromCsvCommand>
{
    public ImportBlockFromCsvCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LanguageId).GreaterThan((short)0);
        RuleFor(x => x.CsvContent).NotEmpty().MaximumLength(600_000);
    }
}
