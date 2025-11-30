using Piece.Data.Enums;

namespace Piece.DTOs
{
	public class LikedTrackViewModel
	{
		public string Id { get; set; } = "";
		public string Title { get; set; } = "";
		public string ArtistName { get; set; } = "";
		public string? AlbumImage { get; set; }
		public string AudioUrl { get; set; } = "";
		public TrackSource Source { get; set; }
		public int? LocalTrackId { get; set; }
		public string? ExternalId { get; set; }
		public DateTime LikedAt { get; set; }
	}
}
