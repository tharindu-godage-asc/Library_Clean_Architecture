using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Queries.GetMemberById
{
    public sealed class GetMemberByIdHandler
        : IRequestHandler<GetMemberByIdQuery, Result<MemberResponse>>
    {
        private readonly IMemberRepository _memberRepository;

        public GetMemberByIdHandler(IMemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }

        public async Task<Result<MemberResponse>> Handle(
            GetMemberByIdQuery request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.Id);

            return member is null
                ? Result.Failure<MemberResponse>(DomainErrors.Member.NotFound(request.Id))
                : Result.Success(member.ToResponse());
        }
    }
}
