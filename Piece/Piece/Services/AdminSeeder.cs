using Microsoft.AspNetCore.Identity;
using Piece.Data;

namespace Piece.Services
{
	public class AdminSeeder
	{
		public static async Task SeedAdminRoleAndUser(
			RoleManager<IdentityRole> roleManager,
			UserManager<ApplicationUser> userManager)
		{
			// Create Admin role if it doesn't exist
			if (!await roleManager.RoleExistsAsync("Admin"))
			{
				await roleManager.CreateAsync(new IdentityRole("Admin"));
				Console.WriteLine("✓ Admin role created");
			}

			// Create default admin user (change these credentials!)
			var adminEmail = "admin@piece.com";
			var adminPassword = "Admin123!"; // CHANGE THIS!

			var adminUser = await userManager.FindByEmailAsync(adminEmail);
			if (adminUser == null)
			{
				adminUser = new ApplicationUser
				{
					UserName = adminEmail,
					Email = adminEmail,
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(adminUser, adminPassword);
				if (result.Succeeded)
				{
					await userManager.AddToRoleAsync(adminUser, "Admin");
					Console.WriteLine($"✓ Admin user created: {adminEmail}");
				}
			}
		}
	}
}
