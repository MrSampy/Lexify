using AutoMapper;
using Lexify.Application.Blocks.Dtos;
using Lexify.Domain.Entities;

namespace Lexify.Application.Blocks.Mappings;

public sealed class BlockMappingProfile : Profile
{
    public BlockMappingProfile()
    {
        CreateMap<WordBlock, WordBlockDto>()
            .ConstructUsing((src, _) => new WordBlockDto(
                src.Id,
                src.UserId,
                src.LanguageId,
                src.Title,
                src.Description,
                src.WordCount,
                src.CreatedAt,
                src.UpdatedAt,
                []));
    }
}
