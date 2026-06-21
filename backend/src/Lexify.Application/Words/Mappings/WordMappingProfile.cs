using AutoMapper;
using Lexify.Application.Words.Dtos;
using Lexify.Domain.Entities;

namespace Lexify.Application.Words.Mappings;

public sealed class WordMappingProfile : Profile
{
    public WordMappingProfile()
    {
        CreateMap<Word, WordDto>();
    }
}
