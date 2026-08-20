using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Auth.Commands.Register
{
    public sealed class RegisterCommandHandler
        : ICommandHandler<RegisterCommand, MemberResponse>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            IMemberRepository memberRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<RegisterCommandHandler> logger)
        {
            _memberRepository = memberRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<MemberResponse>> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var existingMember = await _memberRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (existingMember is not null)
            {
                _logger.LogWarning(
                    "Registration rejected: email {Email} already registered",
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

            member.SetPassword(_passwordHasher.Hash(member, request.Password));

            await _memberRepository.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Member {MemberId} registered",
                member.Id);

            return Result.Success(member.ToResponse());
        }
    }
}
