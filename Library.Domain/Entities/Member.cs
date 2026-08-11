using System;

namespace Library.Domain.Entities
{
    public class Member
    {
        public int Id { get; private set; }

        public string Name { get; private set; } = default!;

        public string Email { get; private set; } = default!;

        public string PhoneNumber { get; private set; } = default!;

        public bool IsActive { get; private set; } = true;

        private Member() { } // Required by EF Core

        public Member(
            string name,
            string email,
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required.");

            Name = name;
            Email = email;
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