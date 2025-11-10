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

		// DbSets for your custom entities
		public DbSet<Track> Tracks { get; set; }
		public DbSet<Playlist> Playlists { get; set; }
		public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
		public DbSet<UserFavorites> UserTrackLikes { get; set; }
		public DbSet<PlayHistory> PlayHistories { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Configure PlaylistTrack relationships
			builder.Entity<PlaylistTrack>()
				.HasOne(pt => pt.Playlist)
				.WithMany(p => p.PlaylistTracks)
				.HasForeignKey(pt => pt.PlaylistId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<PlaylistTrack>()
				.HasOne(pt => pt.Track)
				.WithMany(t => t.PlaylistTracks)
				.HasForeignKey(pt => pt.TrackId)
				.OnDelete(DeleteBehavior.Restrict); // Don't delete tracks if in playlists

			// Configure UserTrackLike relationships
			builder.Entity<UserFavorites>()
				.HasOne(utl => utl.User)
				.WithMany(u => u.LikedTracks)
				.HasForeignKey(utl => utl.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<UserFavorites>()
				.HasOne(utl => utl.Track)
				.WithMany(t => t.LikedByUsers)
				.HasForeignKey(utl => utl.TrackId)
				.OnDelete(DeleteBehavior.Cascade);

			// Configure PlayHistory relationships
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

			// Configure Playlist relationships
			builder.Entity<Playlist>()
				.HasOne(p => p.User)
				.WithMany(u => u.Playlists)
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// Add index for faster queries
			builder.Entity<Track>()
				.HasIndex(t => t.JamendoTrackId);

			builder.Entity<Track>()
				.HasIndex(t => t.Source);
		}
	}
}
