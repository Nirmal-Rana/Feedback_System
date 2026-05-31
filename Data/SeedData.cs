using CollegeIssueManagement.Data;
using CollegeIssueManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace CollegeIssueSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // Create roles
                string[] roles = { "Admin", "Student" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Create admin user
                var adminEmail = "admin@texascollege.edu.np";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "Administrator",
                        EmailConfirmed = true,
                        IsStudent = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    var result = await userManager.CreateAsync(admin, "Admin@1991");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }

                // Seed Admin table (for session-based authentication)
                if (!context.Admins.Any())
                {
                    context.Admins.Add(new Admin
                    {
                        Username = "admin",
                        Password = "admin@1991",
                        Email = "admin@texascollege.edu.np",
                        FullName = "Administrator",
                        LastLogin = DateTime.Now
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}