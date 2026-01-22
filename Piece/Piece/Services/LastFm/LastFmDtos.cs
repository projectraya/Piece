using System.Text.Json.Serialization;

namespace Piece.Services.LastFm
{
	public class LastFmTopArtistsResponse
	{
		[JsonPropertyName("topartists")]
		public TopArtists TopArtists { get; set; } = null!;
	}

	public class TopArtists
	{
		[JsonPropertyName("artist")]
		public List<LastFmArtist> Artist { get; set; } = new();
	}

	public class LastFmArtist
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;
	}

	public class LastFmTopTracksResponse
	{
		[JsonPropertyName("toptracks")]
		public TopTracks TopTracks { get; set; } = null!;
	}

	public class TopTracks
	{
		[JsonPropertyName("track")]
		public List<LastFmTrack> Track { get; set; } = new();
	}

	public class LastFmTrack
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("playcount")]
		public int Playcount { get; set; }

		[JsonPropertyName("artist")]
		public LastFmTrackArtist Artist { get; set; } = null!;
	}

	public class LastFmTrackArtist
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;
	}
}
