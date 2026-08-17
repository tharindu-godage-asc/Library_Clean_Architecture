using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;

namespace Library.Application.Members.Commands.CreateMember
{
    public sealed class CreateMemberCommandHandler
        : ICommandHandler<CreateMemberCommand, MemberResponse>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateMemberCommandHandler(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<MemberResponse>> Handle(
            CreateMemberCommand request,
            CancellationToken cancellationToken)
        {
            var existingMember = await _memberRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (existingMember is not null)
                return Result.Failure<MemberResponse>(DomainErrors.Member.EmailAlreadyExists);

            var memberResult = Member.Create(
                request.Name,
                request.Email,
                request.PhoneNumber);

            if (memberResult.IsFailure)
                return Result.Failure<MemberResponse>(memberResult.Error);

            var member = memberResult.Value;

            await _memberRepository.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(member.ToResponse());
        }
    }
}
