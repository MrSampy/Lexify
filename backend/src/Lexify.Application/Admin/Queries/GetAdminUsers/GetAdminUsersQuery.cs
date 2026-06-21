using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    string? Role,
    string? Status,
    string? Email,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<AdminUserDto>>>;
