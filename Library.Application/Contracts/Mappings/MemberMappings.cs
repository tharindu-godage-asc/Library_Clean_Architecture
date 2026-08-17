using Library.Application.Contracts.Members;
using Library.Domain.Entities;

namespace Library.Application.Contracts.Mappings
{
    public static class MemberMappings
    {
        public static MemberResponse ToResponse(this Member member)
        {
            return new MemberResponse
            {
                Id = member.Id,
                Name = member.Name,
                Email = member.Email.Value,
                PhoneNumber = member.PhoneNumber,
                IsActive = member.IsActive
            };
        }
    }
}