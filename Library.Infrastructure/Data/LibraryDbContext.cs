using Library.Application.Identity;
using Library.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Data
{
    public class LibraryDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public LibraryDbContext(
            DbContextOptions<LibraryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books => Set<Book>();

        public DbSet<Member> Members => Set<Member>();

        public DbSet<Borrowing> Borrowings => Set<Borrowing>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Isbn)
                .IsUnique();

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Title)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.Author)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.PublishedYear);

            modelBuilder.Entity<Member>().OwnsOne(m => m.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("Email")
                    .IsRequired();

                email.HasIndex(e => e.Value)
                    .IsUnique();
            });
        }
    }
}
