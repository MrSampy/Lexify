using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    string? Role,
    string? Status,
    string? Email,
    /// <summary>Null = no filter; true/false narrows to confirmed / unconfirmed addresses.</summary>
    bool? EmailVerified = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<AdminUserDto>>>;
