using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;
using Library.Domain.Shared;

namespace Library.Application.Members.Queries.GetMemberById
{
    public sealed record GetMemberByIdQuery(Guid Id) : IQuery<Result<MemberResponse>>;
}
