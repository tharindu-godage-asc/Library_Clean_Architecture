using Library.Application.Contracts.Members;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Queries.GetMemberById
{
    public sealed record GetMemberByIdQuery(Guid Id) : IRequest<Result<MemberResponse>>;
}
