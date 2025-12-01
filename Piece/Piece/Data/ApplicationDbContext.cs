using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Piece.Data.Models;

namespace Piece.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		// Music player entities
		public DbSet<Track> Tracks { get; set; }
		public DbSet<Genre> Genres { get; set; }
		public DbSet<Playlist> Playlists { get; set; }
		public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
		public DbSet<UserFavorites> UserTrackLikes { get; set; }
		public DbSet<PlayHistory> PlayHistories { get; set; }
		public DbSet<ExternalFavorite> ExternalFavorites { get; set; }
		public DbSet<ListeningHistory> ListeningHistory { get; set; }

		// Map feature entities
		public DbSet<Country> Countries { get; set; }
		public DbSet<Artist> Artists { get; set; }
		public DbSet<ArtistTrack> ArtistTracks { get; set; }

		// Payment entities
		public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
		public DbSet<UserSubscription> UserSubscriptions { get; set; }
		public DbSet<Payment> Payments { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Track-Genre relationship
			builder.Entity<Track>()
				.HasOne(t => t.Genre)
				.WithMany(g => g.Tracks)
				.HasForeignKey(t => t.GenreId)
				.OnDelete(DeleteBehavior.SetNull);

			// Playlist relationships
			builder.Entity<Playlist>()
				.HasOne(p => p.User)
				.WithMany(u => u.Playlists)
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// PlaylistTrack relationships
			builder.Entity<PlaylistTrack>()
				.HasOne(pt => pt.Playlist)
				.WithMany(p => p.PlaylistTracks)
				.HasForeignKey(pt => pt.PlaylistId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PlaylistTrack>()
				.HasOne(pt => pt.Track)
				.WithMany(t => t.PlaylistTracks)
				.HasForeignKey(pt => pt.TrackId)
				.OnDelete(DeleteBehavior.Restrict);

			// UserTrackLike relationships
			builder.Entity<UserFavorites>()
				.HasOne(utl => utl.User)
				.WithMany(u => u.LikedTracks)
				.HasForeignKey(utl => utl.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<UserFavorites>()
				.HasOne(utl => utl.Track)
				.WithMany(t => t.UserTrackLikes)
				.HasForeignKey(utl => utl.TrackId)
				.OnDelete(DeleteBehavior.Cascade);

			// PlayHistory relationships
			builder.Entity<PlayHistory>()
				.HasOne(ph => ph.User)
				.WithMany(u => u.PlayHistory)
				.HasForeignKey(ph => ph.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PlayHistory>()
				.HasOne(ph => ph.Track)
				.WithMany(t => t.PlayHistories)
				.HasForeignKey(ph => ph.TrackId)
				.OnDelete(DeleteBehavior.Cascade);

			// Artist-Country relationship
			builder.Entity<Artist>()
				.HasOne(a => a.Country)
				.WithMany(c => c.Artists)
				.HasForeignKey(a => a.CountryId)
				.OnDelete(DeleteBehavior.Restrict);

			// ArtistTrack relationships
			builder.Entity<ArtistTrack>()
				.HasOne(at => at.Artist)
				.WithMany(a => a.ArtistTracks)
				.HasForeignKey(at => at.ArtistId)
				.OnDelete(DeleteBehavior.Cascade);

			// UserSubscription relationships
			builder.Entity<UserSubscription>()
				.HasOne(us => us.User)
				.WithMany(u => u.Subscriptions)
				.HasForeignKey(us => us.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<UserSubscription>()
				.HasOne(us => us.SubscriptionPlan)
				.WithMany(sp => sp.UserSubscriptions)
				.HasForeignKey(us => us.SubscriptionPlanId)
				.OnDelete(DeleteBehavior.Restrict);

			// Payment relationships
			builder.Entity<Payment>()
				.HasOne(p => p.User)
				.WithMany(u => u.Payments)
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<Payment>()
				.HasOne(p => p.UserSubscription)
				.WithMany(us => us.Payments)
				.HasForeignKey(p => p.UserSubscriptionId)
				.OnDelete(DeleteBehavior.NoAction);

			// Indexes for performance
			builder.Entity<Track>()
				.HasIndex(t => t.JamendoTrackId);

			builder.Entity<Track>()
				.HasIndex(t => t.Source);

			builder.Entity<Country>()
				.HasIndex(c => c.CountryCode)
				.IsUnique();

			builder.Entity<Artist>()
				.HasIndex(a => a.MusicBrainzId);

			builder.Entity<Artist>()
				.HasIndex(a => a.SpotifyId);

			builder.Entity<UserFavorites
				>()
				.HasIndex(utl => new { utl.UserId, utl.TrackId })
				.IsUnique();
		}
	}

}
