using System;
using Library.Domain.Primitives;
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

        public Member(
            string name,
            string email,
            string phoneNumber)
            : base(Guid.NewGuid())
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required.");

            Name = name;
            Email = Library.Domain.ValueObjects.Email.Create(email);
            PhoneNumber = phoneNumber;
            IsActive = true;
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