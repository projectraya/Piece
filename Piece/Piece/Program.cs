using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Piece.Client.Pages;
using Piece.Components;
using Piece.Components.Account;
using Piece.Data;
using Piece.Services;
using Piece.Services.LastFm;
using System.Threading.RateLimiting;


namespace Piece
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddRazorComponents()
				.AddInteractiveServerComponents()
				.AddInteractiveWebAssemblyComponents();

			builder.Services.AddScoped<DatabaseSeeder>();

			builder.Services.AddScoped<IPlaylistService, PlaylistService>();

			builder.Services.AddCascadingAuthenticationState();
			builder.Services.AddScoped<IdentityUserAccessor>();
			builder.Services.AddScoped<IdentityRedirectManager>();
			builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
			builder.Services.AddScoped<PlayerService>();
			builder.Services.AddScoped<IFavoriteService, FavoriteService>();
			builder.Services.AddScoped<IListeningHistoryService, ListeningHistoryService>();
			builder.Services.AddScoped<IProfileService, ProfileService>();
			builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
			builder.Services.AddScoped<IProfanityFilter, ProfanityFilter>();
			builder.Services.AddScoped<IInputSanitizer, InputSanitizer>();
			builder.Services.AddScoped<CountryMusicService>();
			builder.Services.AddHttpClient<MusicBrainzService>();
			builder.Services.AddHttpClient<LastFmService>();
			builder.Services.AddScoped<LastFmSeeder>();


			builder.Services.AddAuthentication(options =>
			{
				options.DefaultScheme = IdentityConstants.ApplicationScheme;
				options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
			})
				.AddIdentityCookies();

			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
			builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));
			builder.Services.AddDatabaseDeveloperPageExceptionFilter();

			builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
				.AddRoles<IdentityRole>()
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddSignInManager()
				.AddDefaultTokenProviders();

			builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
			builder.Services.AddHttpClient<JamendoService>();
			builder.Services.AddRateLimiter(options =>
			{
				options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
					RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
						factory: partition => new FixedWindowRateLimiterOptions
						{
							AutoReplenishment = true,
							PermitLimit = 100,
							QueueLimit = 0,
							Window = TimeSpan.FromMinutes(1)
						}));

				options.AddFixedWindowLimiter("uploads", options =>
				{
					options.PermitLimit = 5;
					options.Window = TimeSpan.FromMinutes(10);
					options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
					options.QueueLimit = 2;
				});

				options.RejectionStatusCode = 429;
			});

			var app = builder.Build();

			var shouldSeed = builder.Configuration.GetValue<bool>("SeedDatabase");

			if (shouldSeed)
			{
				using var scope = app.Services.CreateScope();
				var services = scope.ServiceProvider;

				Console.WriteLine("🌱 Database seeding enabled");

				var context = services.GetRequiredService<ApplicationDbContext>();
				await CountrySeeder.SeedCountriesAsync(context);

				var seeder = services.GetRequiredService<DatabaseSeeder>();
				await seeder.SeedAllAsync();

				await seeder.SeedCountryMusicDataAsync();

				var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
				var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
				await AdminSeeder.SeedAdminRoleAndUser(roleManager, userManager);

				Console.WriteLine("✅ Database seeding finished");
			}
			else
			{
				Console.WriteLine("⏭️ Database seeding skipped");
			}



			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseWebAssemblyDebugging();
				app.UseMigrationsEndPoint();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.Use(async (context, next) =>
			{
				// Prevent MIME type sniffing
				context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

				// Prevent clickjacking
				context.Response.Headers.Append("X-Frame-Options", "DENY");

				// Enable XSS protection
				context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

				// Content Security Policy - WITH D3.JS AND CDN SUPPORT FOR MAP
				context.Response.Headers.Append("Content-Security-Policy",
					"default-src 'self'; " +
					"script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://unpkg.com; " +
					"style-src 'self' 'unsafe-inline' https://unpkg.com; " +
					"img-src 'self' data: https: blob:; " +
					"font-src 'self' data:; " +
					"connect-src 'self' https://*.tile.openstreetmap.org https://*.basemaps.cartocdn.com https://unpkg.com ws://localhost:* wss://localhost:*; " +
					"media-src 'self' https://mp3l.jamendo.com https://usercontent.jamendo.com; " +
					"frame-src 'none';");

				// Control referrer information
				context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

				await next();
			});

			app.UseStaticFiles();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseMiddleware<Piece.Middleware.BanCheckMiddleware>();
			app.UseAntiforgery();
			app.UseRateLimiter();

			app.MapRazorComponents<App>()
				.AddInteractiveServerRenderMode()
				.AddInteractiveWebAssemblyRenderMode()
				.AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

			app.MapAdditionalIdentityEndpoints();

			app.Run();
		}
	}
}