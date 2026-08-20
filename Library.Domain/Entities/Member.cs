using Library.Domain.Enums;
using Library.Domain.Primitives;
using Library.Domain.Shared;
using Library.Domain.ValueObjects;
using System;

namespace Library.Domain.Entities
{
    public class Member : Entity
    {
        public string Name { get; private set; } = default!;

        public string PasswordHash { get; set; } = string.Empty;

        public Role Role { get; set; } = Role.Member;

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

        public const int MaxActiveBorrowings = 3;

        internal static Result<Member> Create(
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

        internal Result UpdateDetails(
            string name,
            string email,
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure(DomainErrors.Member.NameRequired);

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure(DomainErrors.Member.PhoneNumberRequired);

            var emailResult = Library.Domain.ValueObjects.Email.Create(email);

            if (emailResult.IsFailure)
                return Result.Failure(emailResult.Error);

            Name = name;
            Email = emailResult.Value;
            PhoneNumber = phoneNumber;

            return Result.Success();
        }

        internal void Activate()
        {
            IsActive = true;
        }

        internal void Deactivate()
        {
            IsActive = false;
        }

        internal Result EnsureCanBorrow(int activeBorrowingsCount)
        {
            if (!IsActive)
                return Result.Failure(DomainErrors.Member.Inactive);

            if (activeBorrowingsCount >= MaxActiveBorrowings)
                return Result.Failure(DomainErrors.Borrowing.LimitExceeded);

            return Result.Success();
        }
    }
}
