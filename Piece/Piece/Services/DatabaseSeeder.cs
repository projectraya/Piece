using Piece.Data.Enums;
using Piece.Data.Models;
using Piece.Data;
using Microsoft.EntityFrameworkCore;
using Piece.Services;

public class DatabaseSeeder
{
	private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

	public DatabaseSeeder(IDbContextFactory<ApplicationDbContext> dbFactory)
	{
		_dbFactory = dbFactory;
	}

	public async Task SeedAllAsync()
	{
		using var context = await _dbFactory.CreateDbContextAsync();
		await SeedGenresAsync();

		if (!await context.SubscriptionPlans.AnyAsync())
		{
			await SeedSubscriptionPlansAsync();
		}

		if (!await context.Tracks.AnyAsync())
		{
			await SeedLocalTracksAsync();
		}

		await context.SaveChangesAsync();
	}

	public async Task SeedGenresAsync()
	{
		using var context = await _dbFactory.CreateDbContextAsync();
		var genres = new List<Genre>
	{
		new Genre { Name = "EDM", Description = "Electronic and dance music", Color = "#e91e63" },
		new Genre { Name = "Jazz", Description = "Jazz and smooth jazz", Color = "#f39c12" },
		new Genre { Name = "Dance", Description = "Music to dance to", Color = "#1abc9c" },
		new Genre { Name = "Pop", Description = "Pop music", Color = "#9b59b6" },
		new Genre { Name = "Trance", Description = "Subgenre of trap", Color = "#ffeb3b" },
		new Genre { Name = "Trap", Description = "Bass based", Color = "#3498db" },
		new Genre { Name = "Lofi", Description = "Slow and calm", Color = "#CBC3E3" },
		new Genre { Name = "Bounce", Description = "Bouncy ambient music", Color = "#41dc8e" },
		new Genre { Name = "RNB", Description = "Rock and blues music", Color = "#FF474C" },
		new Genre { Name = "Electronic", Description = "Music created by electric impulses", Color = "#FF00FF" }
	};

		if (!context.Genres.Any())
		{
			await context.Genres.AddRangeAsync(genres);
			await context.SaveChangesAsync();
			Console.WriteLine("Genres seeded with initial colors!");
		}
		else
		{
			Console.WriteLine("Genres already exist - skipping seed to preserve custom colors.");
		}

		Console.WriteLine("Genres seeded/updated with colors!");
	}

	private async Task SeedSubscriptionPlansAsync()
	{
		using var context = await _dbFactory.CreateDbContextAsync();
		var plans = new List<SubscriptionPlan>
			{
				new SubscriptionPlan
				{
					Name = "Free",
					Description = "Access to most features.",
					Price = 0.00m,
					DurationDays = 365,
					CanUseMap = false,
					IsActive = true
				},
				new SubscriptionPlan
				{
					Name = "Premium",
					Description = "Have fun with the world map and learn about music culture around the world!",
					Price = 4m,
					DurationDays = 30,
					CanUseMap = true,
					IsActive = true
				}
			};

		await context.SubscriptionPlans.AddRangeAsync(plans);
		await context.SaveChangesAsync();
	}

	private async Task SeedLocalTracksAsync()
	{
		using var context = await _dbFactory.CreateDbContextAsync();

		var electronicGenre = await context.Genres.FirstAsync(g => g.Name == "Electronic");
		var tranceGenre = await context.Genres.FirstAsync(g => g.Name == "Trance");
		var bounceGenre = await context.Genres.FirstAsync(g => g.Name == "Bounce");
		var lofiGenre = await context.Genres.FirstAsync(g => g.Name == "Lofi");
		var edmGenre = await context.Genres.FirstAsync(g => g.Name == "EDM");


		var tracks = new List<Track>
		{
			new Track
			{
				Title = "Eternal Trance",
				ArtistName = "Nexio",
				AlbumName = "Billion",
				GenreId = tranceGenre.Id,
				YearPublished = 2024,
				DurationSeconds = 127,
				PlayCount = 0,
				Source = TrackSource.Local,
				LocalFilePath = "/music/eternal-trance.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				JamendoTrackId = null,
				IsActive = true
			},
			new Track
			{
				Title = "Innovation",
				ArtistName = "Nexio",
				AlbumName = "Billion",
				GenreId = tranceGenre.Id,
				YearPublished = 2024,
				DurationSeconds = 87,
				PlayCount = 0,
				Source = TrackSource.Local,
				LocalFilePath = "/music/innovation.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				JamendoTrackId = null,
				IsActive = true
			},
			new Track
			{
				Title = "Jungle Waves",
				ArtistName = "Evian",
				AlbumName = "Better",
				DurationSeconds = 131,
				YearPublished = 2023,
				GenreId = bounceGenre.Id,
				Source = TrackSource.Local,
				PlayCount = 0,
				LocalFilePath = "/music/jungle-waves.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				JamendoTrackId = null,
				IsActive = true
			},
			new Track
			{
				Title = "Lofi",
				ArtistName = "Evian",
				AlbumName = "Better",
				DurationSeconds = 151,
				YearPublished = 2022,
				GenreId = lofiGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/lofi.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				JamendoTrackId = null,
				IsActive = true,
				PlayCount = 0
			},
			new Track
			{
				Title = "Neoharmonic Dreams",
				ArtistName = "Porunto",
				AlbumName= "Ease",
				DurationSeconds = 387,
				GenreId = edmGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/neoharmonic-dreams.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				PlayCount = 0,
				JamendoTrackId = null,
				IsActive = true,
				YearPublished = 2025
			},
			new Track
			{
				Title = "Pomegranate juice",
				ArtistName = "Chers",
				AlbumName = "Fruit",
				DurationSeconds = 94,
				GenreId = bounceGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/pomegranate-juice.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				PlayCount = 0,
				JamendoTrackId = null,
				IsActive = true,
				YearPublished = 2024
			},
			new Track
			{
				Title = "Retro Lounge",
				ArtistName = "Porunto",
				AlbumName = "Escape",
				DurationSeconds = 106,
				GenreId = edmGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/retro-lounge.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				PlayCount = 0,
				JamendoTrackId = null,
				IsActive = true,
				YearPublished = 2025
			},
			new Track
			{
				Title = "Rise of the Star",
				ArtistName = "Havas",
				AlbumName = "Drumio",
				DurationSeconds = 165,
				GenreId = tranceGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/rise-of-the-star.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				PlayCount = 0,
				JamendoTrackId = null,
				IsActive = true,
				YearPublished = 2024
			},
			new Track
			{
				Title = "Road to Nowhere",
				ArtistName = "Evian",
				AlbumName = "Shrink",
				DurationSeconds = 215,
				GenreId = edmGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/road-to-nowhere.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				PlayCount = 0,
				JamendoTrackId = null,
				IsActive = true,
				YearPublished = 2023
			},
			new Track
			{
				Title = "The Force",
				ArtistName = "Havas",
				AlbumName = "Drumio",
				DurationSeconds = 234,
				GenreId = electronicGenre.Id,
				Source = TrackSource.Local,
				LocalFilePath = "/music/the-force.mp3",
				CoverImageUrl = "/images/default-album-cover.png",
				PlayCount = 0,
				JamendoTrackId = null,
				IsActive = true,
				YearPublished = 2024
			}


		};

		// Calculate hash for each track
		var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
		foreach (var track in tracks)
		{
			var fullPath = Path.Combine(wwwrootPath, track.LocalFilePath.TrimStart('/'));

			if (File.Exists(fullPath))
			{
				track.FileHash = FileValidator.CalculateFileHash(fullPath);
				Console.WriteLine($"[Seeder] Calculated hash for '{track.Title}': {track.FileHash}");
			}
			else
			{
				Console.WriteLine($"[Seeder] WARNING: File not found: {fullPath}");
			}
		}

		await context.Tracks.AddRangeAsync(tracks);
		await context.SaveChangesAsync();

		Console.WriteLine($"[Seeder] Seeded {tracks.Count} tracks with file hashes!");
	}
}