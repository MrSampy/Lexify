using AutoMapper;
using Lexify.Application.Blocks.Dtos;
using Lexify.Domain.Entities;

namespace Lexify.Application.Blocks.Mappings;

public sealed class BlockMappingProfile : Profile
{
    public BlockMappingProfile()
    {
        CreateMap<WordBlock, WordBlockDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(_ => new List<string>()));
    }
}
