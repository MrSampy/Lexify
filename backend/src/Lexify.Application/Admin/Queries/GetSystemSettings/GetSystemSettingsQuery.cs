using Lexify.Application.Admin.Dtos;
using Lexify.Application.Behaviors;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetSystemSettings;

public sealed record GetSystemSettingsQuery : IRequest<Result<IReadOnlyList<SystemSettingDto>>>, ICacheable
{
    public string CacheKey => "admin:settings";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);
}
