using Lexify.Application.Admin.Dtos;
using Lexify.Application.Common;
using Lexify.Domain.Repositories;
using MediatR;

namespace Lexify.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IAdminUserRepository adminUserRepository)
    : IRequestHandler<GetAdminUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(
        GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var (total, entries) = await adminUserRepository.GetPagedWithStatsAsync(
            request.Role, request.Status, request.Email,
            request.Page, request.PageSize, cancellationToken);

        var dtos = entries
            .Select(e => new AdminUserDto(
                e.Id, e.Email, e.DisplayName, e.Role, e.Status,
                e.LastActiveAt, e.CreatedAt, e.BlockCount, e.WordCount, e.TestCount))
            .ToList();

        return Result.Ok(new PagedResult<AdminUserDto>(dtos, total, request.Page, request.PageSize));
    }
}
