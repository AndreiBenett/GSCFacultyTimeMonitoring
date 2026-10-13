using GSCFacultyTimeMonitoring.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GSCFacultyTimeMonitoring.Data;

public class DbInitializer(
    AppDbContext db,
    IConfiguration configuration,
    IPasswordHasher<AdminUser> passwordHasher,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync()
    {
        // Apply database migrations
        await db.Database.MigrateAsync();


        // =========================
        // SEED FACULTY
        // =========================

        if (!await db.Faculty.AnyAsync())
        {
            db.Faculty.AddRange(
                new Faculty
                {
                    FullName = "Juan Dela Cruz",
                    Department = "College Faculty",
                    Status = "Active"
                },
                new Faculty
                {
                    FullName = "Maria Santos",
                    Department = "College Faculty",
                    Status = "Active"
                },
                new Faculty
                {
                    FullName = "Pedro Reyes",
                    Department = "College Faculty",
                    Status = "Active"
                }
            );

            await db.SaveChangesAsync();

            logger.LogInformation("Faculty records created.");
        }



        // =========================
        // ADMIN ACCOUNT
        // =========================

        var username = configuration["AdminSeed:Username"]?.Trim();
        var password = configuration["AdminSeed:Password"];
        var displayName = configuration["AdminSeed:DisplayName"]?.Trim();


        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin credentials are missing in appsettings.json");
        }


        var existingAdmin = await db.AdminUsers
            .FirstOrDefaultAsync(x => x.Username == username);



        if (existingAdmin == null)
        {
            // Create new admin
            var admin = new AdminUser
            {
                Username = username,
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? "GSC Administrator"
                    : displayName,

                IsActive = true,

                // Disable redirect for now
                MustChangePassword = false,

                CreatedAtUtc = DateTimeOffset.UtcNow
            };


            admin.PasswordHash =
                passwordHasher.HashPassword(admin, password);


            db.AdminUsers.Add(admin);

            await db.SaveChangesAsync();


            logger.LogInformation(
                "Admin account created successfully.");
        }
        else
        {
            logger.LogInformation(
                "Administrator already exists. No changes applied.");
        }
    }
}