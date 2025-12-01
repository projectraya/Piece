using Piece.Data.Enums;
using Piece.Data.Models;
using Piece.Data;
using Microsoft.EntityFrameworkCore;

public class DatabaseSeeder
{
	private readonly ApplicationDbContext _context;

	public DatabaseSeeder(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task SeedAllAsync()
	{
		// Check if already seeded
		if (await _context.Genres.AnyAsync())
		{
			return; // Database already has data
		}

		// Seed in order (respecting foreign key dependencies)
		await SeedGenresAsync();
		await SeedSubscriptionPlansAsync();
		await SeedLocalTracksAsync();

		await _context.SaveChangesAsync();
	}

	public async Task SeedGenresAsync()
	{
		var genres = new List<Genre>
	{
		new Genre { Name = "EDM", Description = "Electronic and dance music", Color = "#e91e63" },
		new Genre { Name = "Jazz", Description = "Jazz and smooth jazz", Color = "#f39c12" },
		new Genre { Name = "Dance", Description = "Music to dance to", Color = "#1abc9c" },
		new Genre { Name = "Pop", Description = "Pop music", Color = "#9b59b6" },
		new Genre { Name = "Trance", Description = "Subgenre of trap", Color = "#ffeb3b" },
		new Genre { Name = "Trap", Description = "Bass based", Color = "#3498db" },
		new Genre { Name = "Lofi", Description = "Slow and calm", Color = "#CBC3E3" },
		new Genre { Name = "Bounce", Description = "Bouncy ambient music", Color = "#41dc8e" }, // Added # here
        new Genre { Name = "RNB", Description = "Rock and blues music", Color = "#FF474C" },
		new Genre { Name = "Electronic", Description = "Music created by electric impulses", Color = "#FF00FF" }
	};

		if (!_context.Genres.Any())
		{
			await _context.Genres.AddRangeAsync(genres); // Added await
			await _context.SaveChangesAsync(); // Added await
		}
		else
		{
			// Update existing genres with colors
			foreach (var genre in genres)
			{
				var existing = _context.Genres.FirstOrDefault(g => g.Name == genre.Name);
				if (existing != null)
				{
					existing.Color = genre.Color;
				}
			}
			await _context.SaveChangesAsync(); // Added await
		}

		Console.WriteLine("Genres seeded/updated with colors!");
	}

	private async Task SeedSubscriptionPlansAsync()
	{
		var plans = new List<SubscriptionPlan>
			{
				new SubscriptionPlan
				{
					Name = "Free",
					Description = "Basic access with ads",
					Price = 0.00m,
					DurationDays = 365, // Free forever
                    CanSkipAds = false,
					CanDownload = false,
					HighQualityAudio = false,
					MaxDevices = 1,
					IsActive = true
				},
				new SubscriptionPlan
				{
					Name = "Premium",
					Description = "Ad-free listening with high quality audio",
					Price = 9.99m,
					DurationDays = 30,
					CanSkipAds = true,
					CanDownload = true,
					HighQualityAudio = true,
					MaxDevices = 3,
					IsActive = true
				}
			};

		await _context.SubscriptionPlans.AddRangeAsync(plans);
		await _context.SaveChangesAsync();
	} 

	private async Task SeedLocalTracksAsync()
	{
		// Get genre IDs (we need these for foreign keys)
		var electronicGenre = await _context.Genres.FirstAsync(g => g.Name == "Electronic");
		var tranceGenre = await _context.Genres.FirstAsync(g => g.Name == "Trance");
		var bounceGenre = await _context.Genres.FirstAsync(g => g.Name == "Bounce");
		var lofiGenre = await _context.Genres.FirstAsync(g => g.Name == "Lofi");
		var edmGenre = await _context.Genres.FirstAsync(g => g.Name == "EDM");
		

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
		
		await _context.Tracks.AddRangeAsync(tracks);
		await _context.SaveChangesAsync();
	}
}