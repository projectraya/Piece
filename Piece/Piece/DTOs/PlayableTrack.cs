using Piece.Data.Enums;
using Piece.Data.Models;
using System.Diagnostics.Eventing.Reader;

namespace Piece.DTOs
{
	public class PlayableTrack
	{
		public string Id { get; set; } = "";
		public string Title { get; set; } = "";
		public string ArtistName { get; set; } = "";
		public string AudioUrl { get; set; } = "";
		public string? AlbumImage { get; set; }
		public TrackSource Source { get; set; }
		public bool IsFavorite { get; set; }
		public int? LocalTrackId { get; set; }

		
		public static PlayableTrack FromLocalTrack(Track track, bool isFavorite = false)
		{
			return new PlayableTrack
			{
				Id = $"local-{track.Id}",
				Title = track.Title,
				ArtistName = track.ArtistName,
				AudioUrl = track.LocalFilePath,
				AlbumImage = null,
				Source = TrackSource.Local,
				LocalTrackId = track.Id,
				IsFavorite = isFavorite
			};
		}

		public static PlayableTrack FromJamendoTrack(string id, string name, string artistName, string audioUrl, string? albumImage)
		{
			return new PlayableTrack
			{
				Id = $"jamendo-{id}",
				Title = name,
				ArtistName = artistName,
				AudioUrl = audioUrl,
				AlbumImage = albumImage,
				Source = TrackSource.Jamendo,
				LocalTrackId = null,
				IsFavorite = false
			};
		}
	}
}
