using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Mappings;
using Library.Application.Contracts.Members;
using Library.Application.Identity;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Library.Application.Auth.Commands.Register
{
    public sealed class RegisterCommandHandler
        : ICommandHandler<RegisterCommand, MemberResponse>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            IMemberRepository memberRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<RegisterCommandHandler> logger)
        {
            _memberRepository = memberRepository;
            _userManager = userManager;
            _roleManager = roleManager;
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

            // Tracked here, not saved yet — UserManager.CreateAsync below flushes this
            // Member insert together with the ApplicationUser insert in one SaveChanges
            // call, since both are tracked on the same scoped LibraryDbContext.
            await _memberRepository.AddAsync(member, cancellationToken);

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                MemberId = member.Id
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                _logger.LogWarning(
                    "Registration rejected for {Email}: {Errors}",
                    request.Email,
                    string.Join(", ", createResult.Errors.Select(e => e.Code)));

                if (createResult.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                    return Result.Failure<MemberResponse>(DomainErrors.Member.EmailAlreadyExists);

                var firstError = createResult.Errors.First();
                return Result.Failure<MemberResponse>(Error.Validation(firstError.Code, firstError.Description));
            }

            if (!await _roleManager.RoleExistsAsync(Roles.Member))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Member));

            await _userManager.AddToRoleAsync(user, Roles.Member);

            _logger.LogInformation(
                "Member {MemberId} registered with user {UserId}",
                member.Id,
                user.Id);

            return Result.Success(member.ToResponse());
        }
    }
}
