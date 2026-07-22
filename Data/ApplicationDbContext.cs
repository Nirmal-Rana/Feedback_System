using CollegeIssueManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CollegeIssueManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Issue> Issues { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DocumentAttachment> DocumentAttachments { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AbsenceRecord> AbsenceRecords { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherAssignment> TeacherAssignments { get; set; }
        public DbSet<TeacherFeedback> TeacherFeedbacks { get; set; }
        public DbSet<QRCodeSetting> QRCodeSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Username).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Password).IsRequired().HasMaxLength(255);
                entity.Property(a => a.Email).IsRequired().HasMaxLength(255);
                entity.Property(a => a.FullName).HasMaxLength(200);
                entity.Property(a => a.LastLogin).IsRequired();
            });

            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    Id = 1,
                    Username = "Admin",
                    Password = "Admin@1991",
                    Email = "admin@texascollege.edu.np",
                    FullName = "Administrator",
                    LastLogin = DateTime.Now
                }
            );

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Issue)
                .WithMany(i => i.Notifications)
                .HasForeignKey(n => n.IssueId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentAttachment>()
                .HasOne(d => d.Issue)
                .WithMany()
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.Cascade);

            // NEW — a teacher owns many assignments; deleting the teacher deletes its assignments.
            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(a => a.Teacher)
                .WithMany(t => t.Assignments)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}