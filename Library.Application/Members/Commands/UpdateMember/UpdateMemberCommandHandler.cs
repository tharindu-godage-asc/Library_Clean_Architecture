using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Members.Commands.UpdateMember
{
    public sealed class UpdateMemberCommandHandler
        : ICommandHandler<UpdateMemberCommand, MemberResponse>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateMemberCommandHandler> _logger;
        private readonly ICurrentUserService _currentUser;

        public UpdateMemberCommandHandler(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateMemberCommandHandler> logger,
            ICurrentUserService currentUser)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<MemberResponse>> Handle(
            UpdateMemberCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.Id, cancellationToken);

            if (member is null)
            {
                _logger.LogWarning(
                    "Member update rejected: member {MemberId} not found",
                    request.Id);
                return Result.Failure<MemberResponse>(DomainErrors.Member.NotFound(request.Id));
            }

            var existingMemberWithEmail = await _memberRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (existingMemberWithEmail is not null && existingMemberWithEmail.Id != request.Id)
            {
                _logger.LogWarning(
                    "Member update rejected for member {MemberId}: email {Email} already registered",
                    request.Id,
                    request.Email);
                return Result.Failure<MemberResponse>(DomainErrors.Member.EmailAlreadyExists);
            }

            if (request.IsActive.HasValue && request.IsActive.Value != member.IsActive)
            {
                if (_currentUser.IsAdmin)
                {
                    if (request.IsActive.Value)
                        member.Activate();
                    else
                        member.Deactivate();
                }
                else
                {
                    _logger.LogWarning(
                        "Member update: caller {CallerMemberId} attempted to change IsActive on member {MemberId} without admin rights — ignored",
                        _currentUser.MemberId,
                        request.Id);
                }
            }

            var updateResult = member.UpdateDetails(
                request.Name,
                request.Email,
                request.PhoneNumber);

            if (updateResult.IsFailure)
            {
                _logger.LogWarning(
                    "Member update rejected for member {MemberId}: {ErrorCode}",
                    request.Id,
                    updateResult.Error.Code);
                return Result.Failure<MemberResponse>(updateResult.Error);
            }

            _memberRepository.Update(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Member {MemberId} updated",
                member.Id);

            return Result.Success(member.ToResponse());
        }
    }
}
