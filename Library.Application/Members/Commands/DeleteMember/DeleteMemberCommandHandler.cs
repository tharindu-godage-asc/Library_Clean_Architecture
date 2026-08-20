using Library.Application.Abstractions.Messaging;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Members.Commands.DeleteMember
{
    public sealed class DeleteMemberCommandHandler : ICommandHandler<DeleteMemberCommand>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteMemberCommandHandler> _logger;

        public DeleteMemberCommandHandler(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteMemberCommandHandler> logger)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteMemberCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.Id, cancellationToken);

            if (member is null)
            {
                _logger.LogWarning(
                    "Member deletion rejected: member {MemberId} not found",
                    request.Id);
                return Result.Failure(DomainErrors.Member.NotFound(request.Id));
            }

            _memberRepository.Delete(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Member {MemberId} deleted",
                request.Id);

            return Result.Success();
        }
    }
}
