using Microsoft.EntityFrameworkCore;
using Piece.Data.Models;
using Piece.Data;
using Piece.Data.Enums;

namespace Piece.Services.LastFm
{
	public class LastFmSeeder
	{
		private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
		private readonly LastFmService _lastFm;

		public LastFmSeeder(
			IDbContextFactory<ApplicationDbContext> dbFactory,
			LastFmService lastFm)
		{
			_dbFactory = dbFactory;
			_lastFm = lastFm;
		}

		public async Task SeedCountryTracksAsync()
		{
			Console.WriteLine("🎧 Track seeding skipped - waiting for proper API integration");
			Console.WriteLine("   Artists are seeded and ready to display on the map!");
		}

		public async Task SeedCountryArtistsAsync()
		{
			using var db = await _dbFactory.CreateDbContextAsync();
			var countries = await db.Countries.ToListAsync();

			foreach (var country in countries)
			{
				Console.WriteLine($"🎤 Seeding top artists for {country.Name}");

				try
				{
					var artistNames = await _lastFm.GetTopArtistsAsync(country.Name, 30);

					if (!artistNames.Any())
					{
						Console.WriteLine($"  ⚠️ No artists returned for {country.Name}");
						continue;
					}

					int added = 0;
					int rank = artistNames.Count; 

					foreach (var artistName in artistNames)
					{
						var exists = await db.Artists.AnyAsync(a =>
							a.Name == artistName && a.CountryId == country.Id);

						if (exists)
						{
							rank--;
							continue;
						}

						db.Artists.Add(new Artist
						{
							Name = artistName,
							CountryId = country.Id,
							DataSource = ArtistDataSource.LastFm,
							Popularity = rank, 
							CreatedAt = DateTime.UtcNow
						});
						added++;
						rank--; 
					}

					await db.SaveChangesAsync();
					Console.WriteLine($"  ✅ Added {added} artists for {country.Name}");

					// Last.fm rate limiting
					await Task.Delay(300);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  ❌ Error seeding {country.Name}: {ex.Message}");
				}
			}

			Console.WriteLine("🎉 Artist seeding complete!");
		}
	}
}