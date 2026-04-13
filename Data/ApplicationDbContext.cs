using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PingCRM.Models;

namespace PingCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("accounts");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(25);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(25);
                entity.Property(e => e.PhotoPath).IsRequired(false);
                entity.HasIndex(e => e.DeletedAt);

                entity.HasOne(e => e.Account)
                    .WithMany(a => a.Users)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("organizations");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Address).HasMaxLength(150);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.Region).HasMaxLength(50);
                entity.Property(e => e.Country).HasMaxLength(2);
                entity.Property(e => e.PostalCode).HasMaxLength(25);
                entity.HasIndex(e => e.DeletedAt);

                entity.HasOne(e => e.Account)
                    .WithMany(a => a.Organizations)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("contacts");
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(25);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(25);
                entity.Property(e => e.Email).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Address).HasMaxLength(150);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.Region).HasMaxLength(50);
                entity.Property(e => e.Country).HasMaxLength(2);
                entity.Property(e => e.PostalCode).HasMaxLength(25);
                entity.HasIndex(e => e.DeletedAt);

                entity.HasOne(e => e.Account)
                    .WithMany(a => a.Contacts)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Contacts)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.ToTable("user_sessions");
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => e.UserId);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // Static password hash for "Secret1234" - pre-generated hash
            const string staticPasswordHash = "AQAAAAIAAYagAAAAEFN0yt2nx5dqKiawa4Q0LuW70E9wRZ1sUk8mgdMxGLpDUzwlva43tq8iJ4RP5QCkXw==";
            // Static security stamp - fixed GUID for seeding
            const string staticSecurityStamp = "70e9a4fe-5125-413d-bb98-13a1bdd72fe3";

            modelBuilder.Entity<Account>().HasData(
                new Account { Id = 1, Name = "Acme Corporation", CreatedAt = seedDate, UpdatedAt = seedDate }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    AccountId = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "johndoe@example.com",
                    UserName = "johndoe@example.com",
                    NormalizedUserName = "JOHNDOE@EXAMPLE.COM",
                    NormalizedEmail = "JOHNDOE@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = staticPasswordHash,
                    SecurityStamp = staticSecurityStamp,
                    Owner = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            );

            // Seed Organizations
            modelBuilder.Entity<Organization>().HasData(
                new Organization
                {
                    Id = 1,
                    AccountId = 1,
                    Name = "Tech Solutions Inc",
                    Email = "info@techsolutions.com",
                    Phone = "+1 555-0123",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Organization
                {
                    Id = 2,
                    AccountId = 1,
                    Name = "Global Marketing Group",
                    Email = "contact@globalmarketing.com",
                    Phone = "+1 555-0456",
                    Address = "456 Marketing Avenue",
                    City = "New York",
                    Region = "NY",
                    Country = "US",
                    PostalCode = "10001",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Organization
                {
                    Id = 3,
                    AccountId = 1,
                    Name = "Design Studio Pro",
                    Email = "hello@designstudio.com",
                    Phone = "+1 555-0789",
                    Address = "789 Creative Lane",
                    City = "Los Angeles",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "90210",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Organization
                {
                    Id = 4,
                    AccountId = 1,
                    Name = "Consulting Partners Ltd",
                    Email = "info@consulting.com",
                    Phone = "+1 555-0321",
                    Address = "321 Business Plaza",
                    City = "Chicago",
                    Region = "IL",
                    Country = "US",
                    PostalCode = "60601",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Organization
                {
                    Id = 5,
                    AccountId = 1,
                    Name = "Innovation Labs",
                    Email = "contact@innovationlabs.com",
                    Phone = "+1 555-0654",
                    Address = "654 Innovation Drive",
                    City = "Austin",
                    Region = "TX",
                    Country = "US",
                    PostalCode = "73301",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            );

            // Seed Contacts
            modelBuilder.Entity<Contact>().HasData(
                new Contact
                {
                    Id = 1,
                    AccountId = 1,
                    OrganizationId = 1,
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.johnson@techsolutions.com",
                    Phone = "+1 555-0111",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 2,
                    AccountId = 1,
                    OrganizationId = 1,
                    FirstName = "Michael",
                    LastName = "Chen",
                    Email = "michael.chen@techsolutions.com",
                    Phone = "+1 555-0222",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 3,
                    AccountId = 1,
                    OrganizationId = 2,
                    FirstName = "Emily",
                    LastName = "Rodriguez",
                    Email = "emily.rodriguez@globalmarketing.com",
                    Phone = "+1 555-0333",
                    Address = "456 Marketing Avenue",
                    City = "New York",
                    Region = "NY",
                    Country = "US",
                    PostalCode = "10001",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 4,
                    AccountId = 1,
                    OrganizationId = 3,
                    FirstName = "David",
                    LastName = "Wilson",
                    Email = "david.wilson@designstudio.com",
                    Phone = "+1 555-0444",
                    Address = "789 Creative Lane",
                    City = "Los Angeles",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "90210",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 5,
                    AccountId = 1,
                    OrganizationId = 4,
                    FirstName = "Lisa",
                    LastName = "Anderson",
                    Email = "lisa.anderson@consulting.com",
                    Phone = "+1 555-0555",
                    Address = "321 Business Plaza",
                    City = "Chicago",
                    Region = "IL",
                    Country = "US",
                    PostalCode = "60601",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 6,
                    AccountId = 1,
                    OrganizationId = 5,
                    FirstName = "Robert",
                    LastName = "Taylor",
                    Email = "robert.taylor@innovationlabs.com",
                    Phone = "+1 555-0666",
                    Address = "654 Innovation Drive",
                    City = "Austin",
                    Region = "TX",
                    Country = "US",
                    PostalCode = "73301",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 7,
                    AccountId = 1,
                    OrganizationId = null,
                    FirstName = "Jennifer",
                    LastName = "Brown",
                    Email = "jennifer.brown@freelancer.com",
                    Phone = "+1 555-0777",
                    Address = "987 Independent Way",
                    City = "Seattle",
                    Region = "WA",
                    Country = "US",
                    PostalCode = "98101",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 8,
                    AccountId = 1,
                    OrganizationId = null,
                    FirstName = "James",
                    LastName = "Miller",
                    Email = "james.miller@consultant.com",
                    Phone = "+1 555-0888",
                    Address = "147 Solo Street",
                    City = "Denver",
                    Region = "CO",
                    Country = "US",
                    PostalCode = "80201",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 9,
                    AccountId = 1,
                    OrganizationId = 2,
                    FirstName = "Patricia",
                    LastName = "Garcia",
                    Email = "patricia.garcia@globalmarketing.com",
                    Phone = "+1 555-0901",
                    Address = "456 Marketing Avenue",
                    City = "New York",
                    Region = "NY",
                    Country = "US",
                    PostalCode = "10001",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 10,
                    AccountId = 1,
                    OrganizationId = 3,
                    FirstName = "Christopher",
                    LastName = "Martinez",
                    Email = "chris.martinez@designstudio.com",
                    Phone = "+1 555-1001",
                    Address = "789 Creative Lane",
                    City = "Los Angeles",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "90210",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 11,
                    AccountId = 1,
                    OrganizationId = 1,
                    FirstName = "Amanda",
                    LastName = "Davis",
                    Email = "amanda.davis@techsolutions.com",
                    Phone = "+1 555-1101",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 12,
                    AccountId = 1,
                    OrganizationId = 4,
                    FirstName = "Daniel",
                    LastName = "Lopez",
                    Email = "daniel.lopez@consulting.com",
                    Phone = "+1 555-1201",
                    Address = "321 Business Plaza",
                    City = "Chicago",
                    Region = "IL",
                    Country = "US",
                    PostalCode = "60601",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 13,
                    AccountId = 1,
                    OrganizationId = 5,
                    FirstName = "Michelle",
                    LastName = "Thomas",
                    Email = "michelle.thomas@innovationlabs.com",
                    Phone = "+1 555-1301",
                    Address = "654 Innovation Drive",
                    City = "Austin",
                    Region = "TX",
                    Country = "US",
                    PostalCode = "73301",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 14,
                    AccountId = 1,
                    OrganizationId = null,
                    FirstName = "Kevin",
                    LastName = "Jackson",
                    Email = "kevin.jackson@freelancer.com",
                    Phone = "+1 555-1401",
                    Address = "258 Remote Street",
                    City = "Portland",
                    Region = "OR",
                    Country = "US",
                    PostalCode = "97201",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 15,
                    AccountId = 1,
                    OrganizationId = 2,
                    FirstName = "Laura",
                    LastName = "White",
                    Email = "laura.white@globalmarketing.com",
                    Phone = "+1 555-1501",
                    Address = "456 Marketing Avenue",
                    City = "New York",
                    Region = "NY",
                    Country = "US",
                    PostalCode = "10001",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 16,
                    AccountId = 1,
                    OrganizationId = 1,
                    FirstName = "Brian",
                    LastName = "Harris",
                    Email = "brian.harris@techsolutions.com",
                    Phone = "+1 555-1601",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 17,
                    AccountId = 1,
                    OrganizationId = 3,
                    FirstName = "Samantha",
                    LastName = "Clark",
                    Email = "samantha.clark@designstudio.com",
                    Phone = "+1 555-1701",
                    Address = "789 Creative Lane",
                    City = "Los Angeles",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "90210",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 18,
                    AccountId = 1,
                    OrganizationId = 4,
                    FirstName = "Ryan",
                    LastName = "Lewis",
                    Email = "ryan.lewis@consulting.com",
                    Phone = "+1 555-1801",
                    Address = "321 Business Plaza",
                    City = "Chicago",
                    Region = "IL",
                    Country = "US",
                    PostalCode = "60601",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 19,
                    AccountId = 1,
                    OrganizationId = 5,
                    FirstName = "Rachel",
                    LastName = "Robinson",
                    Email = "rachel.robinson@innovationlabs.com",
                    Phone = "+1 555-1901",
                    Address = "654 Innovation Drive",
                    City = "Austin",
                    Region = "TX",
                    Country = "US",
                    PostalCode = "73301",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 20,
                    AccountId = 1,
                    OrganizationId = null,
                    FirstName = "Steven",
                    LastName = "Walker",
                    Email = "steven.walker@independent.com",
                    Phone = "+1 555-2001",
                    Address = "369 Freelance Lane",
                    City = "Miami",
                    Region = "FL",
                    Country = "US",
                    PostalCode = "33101",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 21,
                    AccountId = 1,
                    OrganizationId = 1,
                    FirstName = "Nicole",
                    LastName = "Hall",
                    Email = "nicole.hall@techsolutions.com",
                    Phone = "+1 555-2101",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 22,
                    AccountId = 1,
                    OrganizationId = 2,
                    FirstName = "Andrew",
                    LastName = "Young",
                    Email = "andrew.young@globalmarketing.com",
                    Phone = "+1 555-2201",
                    Address = "456 Marketing Avenue",
                    City = "New York",
                    Region = "NY",
                    Country = "US",
                    PostalCode = "10001",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 23,
                    AccountId = 1,
                    OrganizationId = 3,
                    FirstName = "Jessica",
                    LastName = "King",
                    Email = "jessica.king@designstudio.com",
                    Phone = "+1 555-2301",
                    Address = "789 Creative Lane",
                    City = "Los Angeles",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "90210",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 24,
                    AccountId = 1,
                    OrganizationId = 4,
                    FirstName = "Joshua",
                    LastName = "Wright",
                    Email = "joshua.wright@consulting.com",
                    Phone = "+1 555-2401",
                    Address = "321 Business Plaza",
                    City = "Chicago",
                    Region = "IL",
                    Country = "US",
                    PostalCode = "60601",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 25,
                    AccountId = 1,
                    OrganizationId = 5,
                    FirstName = "Stephanie",
                    LastName = "Hill",
                    Email = "stephanie.hill@innovationlabs.com",
                    Phone = "+1 555-2501",
                    Address = "654 Innovation Drive",
                    City = "Austin",
                    Region = "TX",
                    Country = "US",
                    PostalCode = "73301",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 26,
                    AccountId = 1,
                    OrganizationId = null,
                    FirstName = "Matthew",
                    LastName = "Scott",
                    Email = "matthew.scott@contractor.com",
                    Phone = "+1 555-2601",
                    Address = "789 Independent Blvd",
                    City = "Boston",
                    Region = "MA",
                    Country = "US",
                    PostalCode = "02101",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 27,
                    AccountId = 1,
                    OrganizationId = 1,
                    FirstName = "Ashley",
                    LastName = "Green",
                    Email = "ashley.green@techsolutions.com",
                    Phone = "+1 555-2701",
                    Address = "123 Tech Street",
                    City = "San Francisco",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "94105",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 28,
                    AccountId = 1,
                    OrganizationId = 2,
                    FirstName = "Justin",
                    LastName = "Adams",
                    Email = "justin.adams@globalmarketing.com",
                    Phone = "+1 555-2801",
                    Address = "456 Marketing Avenue",
                    City = "New York",
                    Region = "NY",
                    Country = "US",
                    PostalCode = "10001",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 29,
                    AccountId = 1,
                    OrganizationId = 3,
                    FirstName = "Megan",
                    LastName = "Baker",
                    Email = "megan.baker@designstudio.com",
                    Phone = "+1 555-2901",
                    Address = "789 Creative Lane",
                    City = "Los Angeles",
                    Region = "CA",
                    Country = "US",
                    PostalCode = "90210",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new Contact
                {
                    Id = 30,
                    AccountId = 1,
                    OrganizationId = 4,
                    FirstName = "Brandon",
                    LastName = "Nelson",
                    Email = "brandon.nelson@consulting.com",
                    Phone = "+1 555-3001",
                    Address = "321 Business Plaza",
                    City = "Chicago",
                    Region = "IL",
                    Country = "US",
                    PostalCode = "60601",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            );
        }
    }
}