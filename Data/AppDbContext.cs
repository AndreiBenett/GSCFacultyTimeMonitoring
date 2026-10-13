using GSCFacultyTimeMonitoring.Models;
using Microsoft.EntityFrameworkCore;

namespace GSCFacultyTimeMonitoring.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Faculty> Faculty => Set<Faculty>();
    public DbSet<TimeInRecord> TimeInRecords => Set<TimeInRecord>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculty");
            entity.HasKey(x => x.FacultyId);
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Department).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.LateAfter).HasColumnType("time(0)");
            entity.HasIndex(x => x.FullName);
        });

        modelBuilder.Entity<TimeInRecord>(entity =>
        {
            entity.ToTable("TimeInRecords");
            entity.HasKey(x => x.RecordId);
            entity.Property(x => x.Date).HasColumnType("date");
            entity.Property(x => x.TimeIn).HasColumnType("time(0)");
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(x => new { x.FacultyId, x.Date }).IsUnique();
            entity.HasIndex(x => x.Date);
            entity.HasOne(x => x.Faculty)
                .WithMany(x => x.TimeInRecords)
                .HasForeignKey(x => x.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUsers");
            entity.HasKey(x => x.AdminUserId);
            entity.Property(x => x.Username).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(x => x.Username).IsUnique();
        });
    }
}
