using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Commands.UpdateMember
{
    public sealed class UpdateMemberHandler
        : IRequestHandler<UpdateMemberCommand, Result<MemberResponse>>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMemberHandler(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<MemberResponse>> Handle(
            UpdateMemberCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.Id, cancellationToken);

            if (member is null)
                return Result.Failure<MemberResponse>(DomainErrors.Member.NotFound(request.Id));

            var existingMemberWithEmail = await _memberRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (existingMemberWithEmail is not null && existingMemberWithEmail.Id != request.Id)
                return Result.Failure<MemberResponse>(DomainErrors.Member.EmailAlreadyExists);

            var updateResult = member.UpdateDetails(
                request.Name,
                request.Email,
                request.PhoneNumber);

            if (updateResult.IsFailure)
                return Result.Failure<MemberResponse>(updateResult.Error);

            _memberRepository.Update(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(member.ToResponse());
        }
    }
}
