using Piece.Data.Models;
using Piece.Data;
using Microsoft.EntityFrameworkCore;

namespace Piece.Services
{
	public class CountrySeeder
	{
		public static async Task SeedCountriesAsync(ApplicationDbContext context)
		{
			if (await context.Countries.AnyAsync())
			{
				Console.WriteLine("🌍 Countries already seeded");
				return;
			}

			var countries = new List<Country>
			{
			    // North America
			    new Country { Name = "United States", CountryCode = "USA", ISO2Code = "US", Latitude = 37.0902, Longitude = -95.7129 },
				new Country { Name = "Canada", CountryCode = "CAN", ISO2Code = "CA", Latitude = 56.1304, Longitude = -106.3468 },
				new Country { Name = "Mexico", CountryCode = "MEX", ISO2Code = "MX", Latitude = 23.6345, Longitude = -102.5528 },
			
			    // South America
			    new Country { Name = "Brazil", CountryCode = "BRA", ISO2Code = "BR", Latitude = -14.2350, Longitude = -51.9253 },
				new Country { Name = "Argentina", CountryCode = "ARG", ISO2Code = "AR", Latitude = -38.4161, Longitude = -63.6167 },
				new Country { Name = "Chile", CountryCode = "CHL", ISO2Code = "CL", Latitude = -35.6751, Longitude = -71.5430 },
				new Country { Name = "Colombia", CountryCode = "COL", ISO2Code = "CO", Latitude = 4.5709, Longitude = -74.2973 },
				new Country { Name = "Peru", CountryCode = "PER", ISO2Code = "PE", Latitude = -9.1900, Longitude = -75.0152 },
				new Country { Name = "Venezuela", CountryCode = "VEN", ISO2Code = "VE", Latitude = 6.4238, Longitude = -66.5897 },
			
			    // Europe
			    new Country { Name = "United Kingdom", CountryCode = "GBR", ISO2Code = "GB", Latitude = 55.3781, Longitude = -3.4360 },
				new Country { Name = "France", CountryCode = "FRA", ISO2Code = "FR", Latitude = 46.2276, Longitude = 2.2137 },
				new Country { Name = "Germany", CountryCode = "DEU", ISO2Code = "DE", Latitude = 51.1657, Longitude = 10.4515 },
				new Country { Name = "Italy", CountryCode = "ITA", ISO2Code = "IT", Latitude = 41.8719, Longitude = 12.5674 },
				new Country { Name = "Spain", CountryCode = "ESP", ISO2Code = "ES", Latitude = 40.4637, Longitude = -3.7492 },
				new Country { Name = "Portugal", CountryCode = "PRT", ISO2Code = "PT", Latitude = 39.3999, Longitude = -8.2245 },
				new Country { Name = "Netherlands", CountryCode = "NLD", ISO2Code = "NL", Latitude = 52.1326, Longitude = 5.2913 },
				new Country { Name = "Belgium", CountryCode = "BEL", ISO2Code = "BE", Latitude = 50.5039, Longitude = 4.4699 },
				new Country { Name = "Switzerland", CountryCode = "CHE", ISO2Code = "CH", Latitude = 46.8182, Longitude = 8.2275 },
				new Country { Name = "Austria", CountryCode = "AUT", ISO2Code = "AT", Latitude = 47.5162, Longitude = 14.5501 },
				new Country { Name = "Sweden", CountryCode = "SWE", ISO2Code = "SE", Latitude = 60.1282, Longitude = 18.6435 },
				new Country { Name = "Norway", CountryCode = "NOR", ISO2Code = "NO", Latitude = 60.4720, Longitude = 8.4689 },
				new Country { Name = "Denmark", CountryCode = "DNK", ISO2Code = "DK", Latitude = 56.2639, Longitude = 9.5018 },
				new Country { Name = "Finland", CountryCode = "FIN", ISO2Code = "FI", Latitude = 61.9241, Longitude = 25.7482 },
				new Country { Name = "Poland", CountryCode = "POL", ISO2Code = "PL", Latitude = 51.9194, Longitude = 19.1451 },
				new Country { Name = "Greece", CountryCode = "GRC", ISO2Code = "GR", Latitude = 39.0742, Longitude = 21.8243 },
				new Country { Name = "Ireland", CountryCode = "IRL", ISO2Code = "IE", Latitude = 53.4129, Longitude = -8.2439 },
				new Country { Name = "Iceland", CountryCode = "ISL", ISO2Code = "IS", Latitude = 64.9631, Longitude = -19.0208 },
			
			    // Asia
			    new Country { Name = "Russia", CountryCode = "RUS", ISO2Code = "RU", Latitude = 61.5240, Longitude = 105.3188 },
				new Country { Name = "China", CountryCode = "CHN", ISO2Code = "CN", Latitude = 35.8617, Longitude = 104.1954 },
				new Country { Name = "Japan", CountryCode = "JPN", ISO2Code = "JP", Latitude = 36.2048, Longitude = 138.2529 },
				new Country { Name = "South Korea", CountryCode = "KOR", ISO2Code = "KR", Latitude = 35.9078, Longitude = 127.7669 },
				new Country { Name = "India", CountryCode = "IND", ISO2Code = "IN", Latitude = 20.5937, Longitude = 78.9629 },
				new Country { Name = "Thailand", CountryCode = "THA", ISO2Code = "TH", Latitude = 15.8700, Longitude = 100.9925 },
				new Country { Name = "Vietnam", CountryCode = "VNM", ISO2Code = "VN", Latitude = 14.0583, Longitude = 108.2772 },
				new Country { Name = "Indonesia", CountryCode = "IDN", ISO2Code = "ID", Latitude = -0.7893, Longitude = 113.9213 },
				new Country { Name = "Philippines", CountryCode = "PHL", ISO2Code = "PH", Latitude = 12.8797, Longitude = 121.7740 },
				new Country { Name = "Malaysia", CountryCode = "MYS", ISO2Code = "MY", Latitude = 4.2105, Longitude = 101.9758 },
				new Country { Name = "Turkey", CountryCode = "TUR", ISO2Code = "TR", Latitude = 38.9637, Longitude = 35.2433 },
			
			    // Middle East & Africa
			    new Country { Name = "Egypt", CountryCode = "EGY", ISO2Code = "EG", Latitude = 26.8206, Longitude = 30.8025 },
				new Country { Name = "South Africa", CountryCode = "ZAF", ISO2Code = "ZA", Latitude = -30.5595, Longitude = 22.9375 },
				new Country { Name = "Nigeria", CountryCode = "NGA", ISO2Code = "NG", Latitude = 9.0820, Longitude = 8.6753 },
				new Country { Name = "Morocco", CountryCode = "MAR", ISO2Code = "MA", Latitude = 31.7917, Longitude = -7.0926 },
			
			    // Oceania
			    new Country { Name = "Australia", CountryCode = "AUS", ISO2Code = "AU", Latitude = -25.2744, Longitude = 133.7751 },
				new Country { Name = "New Zealand", CountryCode = "NZL", ISO2Code = "NZ", Latitude = -40.9006, Longitude = 174.8860 }
			};

			await context.Countries.AddRangeAsync(countries);
			await context.SaveChangesAsync();

			Console.WriteLine($"✅ Seeded {countries.Count} countries");
		}
	}
}
