using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Commands.DeleteMember
{
    public sealed class DeleteMemberCommandHandler : IRequestHandler<DeleteMemberCommand, Result>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteMemberCommandHandler(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(
            DeleteMemberCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.Id, cancellationToken);

            if (member is null)
                return Result.Failure(DomainErrors.Member.NotFound(request.Id));

            _memberRepository.Delete(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
