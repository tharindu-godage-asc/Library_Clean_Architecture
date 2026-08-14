using System;
using Library.Domain.Primitives;
using Library.Domain.Shared;
using Library.Domain.ValueObjects;

namespace Library.Domain.Entities
{
    public class Member : Entity
    {
        public string Name { get; private set; } = default!;

        public Email Email { get; private set; } = default!;

        public string PhoneNumber { get; private set; } = default!;

        public bool IsActive { get; private set; } = true;

        private Member() : base(Guid.Empty) { } // Required by EF Core

        private Member(
            string name,
            Library.Domain.ValueObjects.Email email,
            string phoneNumber)
            : base(Guid.NewGuid())
        {
            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
            IsActive = true;
        }

        public static Result<Member> Create(
            string name,
            string email,
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<Member>(DomainErrors.Member.NameRequired);

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure<Member>(DomainErrors.Member.PhoneNumberRequired);

            var emailResult = Library.Domain.ValueObjects.Email.Create(email);

            if (emailResult.IsFailure)
                return Result.Failure<Member>(emailResult.Error);

            return Result.Success(new Member(name, emailResult.Value, phoneNumber));
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
