using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;

namespace Library.Application.Members.Queries.GetAllMembers
{
    public sealed record GetAllMembersQuery : IQuery<IEnumerable<MemberResponse>>;
}
