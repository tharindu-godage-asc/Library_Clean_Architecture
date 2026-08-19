using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Members.Commands.CreateMember
{
    public sealed class CreateMemberCommandHandler
        : ICommandHandler<CreateMemberCommand, MemberResponse>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateMemberCommandHandler> _logger;

        public CreateMemberCommandHandler(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateMemberCommandHandler> logger)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<MemberResponse>> Handle(
            CreateMemberCommand request,
            CancellationToken cancellationToken)
        {
            var existingMember = await _memberRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (existingMember is not null)
            {
                _logger.LogWarning(
                    "Member registration rejected: email {Email} already registered",
                    request.Email);
                return Result.Failure<MemberResponse>(DomainErrors.Member.EmailAlreadyExists);
            }

            var memberResult = Member.Create(
                request.Name,
                request.Email,
                request.PhoneNumber);

            if (memberResult.IsFailure)
                return Result.Failure<MemberResponse>(memberResult.Error);

            var member = memberResult.Value;

            await _memberRepository.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Member {MemberId} registered",
                member.Id);

            return Result.Success(member.ToResponse());
        }
    }
}
