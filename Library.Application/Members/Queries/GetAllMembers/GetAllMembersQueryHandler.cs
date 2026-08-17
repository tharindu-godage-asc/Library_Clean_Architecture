using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using MediatR;

namespace Library.Application.Members.Queries.GetAllMembers
{
    public sealed class GetAllMembersQueryHandler
        : IRequestHandler<GetAllMembersQuery, IEnumerable<MemberResponse>>
    {
        private readonly IMemberRepository _memberRepository;

        public GetAllMembersQueryHandler(IMemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }

        public async Task<IEnumerable<MemberResponse>> Handle(
            GetAllMembersQuery request,
            CancellationToken cancellationToken)
        {
            var members = await _memberRepository.GetAllAsync(cancellationToken);

            return members.Select(m => m.ToResponse());
        }
    }
}
