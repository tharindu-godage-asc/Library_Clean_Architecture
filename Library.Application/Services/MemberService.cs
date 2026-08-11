using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Services
{
    public class MemberService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MemberService(
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork)
        {
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Member>> GetAllAsync()
        {
            return await _memberRepository.GetAllAsync();
        }

        public async Task<Member> GetByIdAsync(int id)
        {
            return await _memberRepository.GetByIdAsync(id)
                   ?? throw new NotFoundException("Member not found.");
        }

        public async Task CreateAsync(Member member)
        {
            var existingMember =
                await _memberRepository.GetByEmailAsync(member.Email);

            if (existingMember is not null)
                throw new ConflictException(
                    "A member with this email already exists.");

            await _memberRepository.AddAsync(member);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var member = await _memberRepository.GetByIdAsync(id)
                         ?? throw new NotFoundException("Member not found.");

            _memberRepository.Delete(member);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}