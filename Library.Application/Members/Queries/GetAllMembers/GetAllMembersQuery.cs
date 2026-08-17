using Library.Application.Contracts.Members;
using MediatR;

namespace Library.Application.Members.Queries.GetAllMembers
{
    public sealed record GetAllMembersQuery : IRequest<IEnumerable<MemberResponse>>;
}
