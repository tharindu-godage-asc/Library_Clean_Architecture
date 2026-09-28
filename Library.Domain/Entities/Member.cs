using Library.Domain.Primitives;
using Library.Domain.Shared;
using Library.Domain.ValueObjects;
using System;

namespace Library.Domain.Entities
{
    public class Member : Entity
    {
        public string Name { get; private set; } = default!;

        public Email Email { get; private set; } = default!;

        public string PhoneNumber { get; private set; } = default!;

        public bool IsActive { get; private set; } = true;

        public string? KeycloakId { get; private set; }

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

        /// <summary>
        /// JIT-provisioning path for a Keycloak-authenticated identity with no existing Member
        /// record (see MemberProvisioningService). Phone number can't be required here like the
        /// admin-facing Create() factory does — Keycloak's standard OIDC scopes (openid/profile/
        /// email) never carry one — so it's left blank; the member can fill it in later via the
        /// existing PUT /api/members/{id} self-edit endpoint.
        /// </summary>
        internal static Result<Member> CreateFromKeycloak(
            string keycloakId,
            string name,
            string email)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("Keycloak id is required.", nameof(keycloakId));

            var emailResult = Library.Domain.ValueObjects.Email.Create(email);

            if (emailResult.IsFailure)
                return Result.Failure<Member>(emailResult.Error);

            var member = new Member(
                string.IsNullOrWhiteSpace(name) ? email : name,
                emailResult.Value,
                string.Empty)
            {
                KeycloakId = keycloakId
            };

            return Result.Success(member);
        }

        internal void LinkKeycloakId(string keycloakId)
        {
            KeycloakId = keycloakId;
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
