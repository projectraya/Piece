using NUnit.Framework;
using Piece.Services;
using Piece.DTOs;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class PlayerServiceTests
	{
		private PlayerService _service;

		[SetUp]
		public void Setup()
		{
			_service = new PlayerService();
		}

		[Test]
		public void PlayTrack_WhenCalled_SetsCurrentTrack()
		{
			// Arrange
			var track = new PlayableTrack
			{
				Id = "1",
				Title = "Test Track",
				ArtistName = "Test Artist"
			};

			// Act
			_service.PlayTrack(track);

			// Assert
			Assert.That(_service.CurrentTrack, Is.Not.Null);
			Assert.That(_service.CurrentTrack.Id, Is.EqualTo("1"));
			Assert.That(_service.IsPlaying, Is.True);
		}

		[Test]
		public void PlayTrack_AddsTrackToQueue()
		{
			// Arrange
			var track = new PlayableTrack { Id = "1", Title = "Track 1" };

			// Act
			_service.PlayTrack(track);

			// Assert
			Assert.That(_service.Queue.Count, Is.EqualTo(1));
			Assert.That(_service.Queue[0].Id, Is.EqualTo("1"));
		}

		[Test]
		public void PlayPlaylist_LoadsTracksIntoQueue()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1", Title = "Track 1" },
				new() { Id = "2", Title = "Track 2" },
				new() { Id = "3", Title = "Track 3" }
			};

			// Act
			_service.PlayPlaylist(tracks);

			// Assert
			Assert.That(_service.Queue.Count, Is.EqualTo(3));
			Assert.That(_service.CurrentTrack, Is.Not.Null);
			Assert.That(_service.CurrentTrack.Id, Is.EqualTo("1"));
			Assert.That(_service.IsPlaying, Is.True);
		}

		[Test]
		public void PlayPlaylist_WithStartIndex_StartsAtCorrectTrack()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1", Title = "Track 1" },
				new() { Id = "2", Title = "Track 2" },
				new() { Id = "3", Title = "Track 3" }
			};

			// Act
			_service.PlayPlaylist(tracks, startIndex: 1);

			// Assert
			Assert.That(_service.CurrentTrack.Id, Is.EqualTo("2"));
			Assert.That(_service.Queue.Count, Is.EqualTo(3));
		}

		[Test]
		public void PlayNext_MovesToNextTrackInQueue()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1", Title = "Track 1" },
				new() { Id = "2", Title = "Track 2" }
			};
			_service.PlayPlaylist(tracks);

			// Act
			_service.PlayNext();

			// Assert
			Assert.That(_service.CurrentTrack.Id, Is.EqualTo("2"));
			Assert.That(_service.IsPlaying, Is.True);
		}

		[Test]
		public void PlayPrevious_WithHistory_ReturnsToHistoryTrack()
		{
			// Arrange
			var track1 = new PlayableTrack { Id = "1", Title = "Track 1" };
			var track2 = new PlayableTrack { Id = "2", Title = "Track 2" };

			_service.PlayTrack(track1);
			_service.PlayTrack(track2);

			// Act
			_service.PlayPrevious();

			// Assert
			Assert.That(_service.CurrentTrack.Id, Is.EqualTo("1"));
		}

		[Test]
		public void TogglePlayPause_TogglesIsPlaying()
		{
			// Arrange
			var track = new PlayableTrack { Id = "1", Title = "Test" };
			_service.PlayTrack(track);
			var wasPlaying = _service.IsPlaying;

			// Act
			_service.TogglePlayPause();

			// Assert
			Assert.That(_service.IsPlaying, Is.EqualTo(!wasPlaying));
		}

		[Test]
		public void ToggleShuffle_TogglesIsShuffleOn()
		{
			// Arrange
			Assert.That(_service.IsShuffleOn, Is.False);

			// Act
			_service.ToggleShuffle();

			// Assert
			Assert.That(_service.IsShuffleOn, Is.True);
		}

		[Test]
		public void ToggleShuffle_ShufflesQueue()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1", Title = "Track 1" },
				new() { Id = "2", Title = "Track 2" },
				new() { Id = "3", Title = "Track 3" },
				new() { Id = "4", Title = "Track 4" },
				new() { Id = "5", Title = "Track 5" }
			};
			_service.PlayPlaylist(tracks);
			var currentTrackId = _service.CurrentTrack.Id;

			// Act
			_service.ToggleShuffle();

			// Assert
			Assert.That(_service.IsShuffleOn, Is.True);
			Assert.That(_service.Queue.Count, Is.EqualTo(5));
			
			Assert.That(_service.Queue[0].Id, Is.EqualTo(currentTrackId));
		}

		[Test]
		public void AddToQueue_AddsTrackToEnd()
		{
			// Arrange
			var track1 = new PlayableTrack { Id = "1", Title = "Track 1" };
			var track2 = new PlayableTrack { Id = "2", Title = "Track 2" };

			_service.PlayTrack(track1);

			// Act
			_service.AddToQueue(track2);

			// Assert
			Assert.That(_service.Queue.Count, Is.EqualTo(2));
			Assert.That(_service.Queue[1].Id, Is.EqualTo("2"));
		}

		[Test]
		public void RemoveFromQueue_RemovesTrackAtIndex()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1" },
				new() { Id = "2" },
				new() { Id = "3" }
			};
			_service.PlayPlaylist(tracks);

			// Act
			_service.RemoveFromQueue(1);

			// Assert
			Assert.That(_service.Queue.Count, Is.EqualTo(2));
			Assert.That(_service.Queue.Any(t => t.Id == "2"), Is.False);
		}

		[Test]
		public void ReorderQueue_MovesTrackToNewPosition()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1", Title = "Track 1" },
				new() { Id = "2", Title = "Track 2" },
				new() { Id = "3", Title = "Track 3" }
			};
			_service.PlayPlaylist(tracks);

			// Act
			_service.ReorderQueue(0, 2);

			// Assert
			Assert.That(_service.Queue[2].Id, Is.EqualTo("1"));
			Assert.That(_service.Queue[0].Id, Is.EqualTo("2"));
		}

		[Test]
		public void ClearQueue_RemovesAllTracksAndStops()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1" },
				new() { Id = "2" }
			};
			_service.PlayPlaylist(tracks);

			// Act
			_service.ClearQueue();

			// Assert
			Assert.That(_service.Queue.Count, Is.EqualTo(0));
			Assert.That(_service.CurrentTrack, Is.Null);
			Assert.That(_service.IsPlaying, Is.False);
		}

		[Test]
		public void UpdateFavoriteStatus_UpdatesCurrentTrack()
		{
			// Arrange
			var track = new PlayableTrack { Id = "1", IsFavorite = false };
			_service.PlayTrack(track);

			// Act
			_service.UpdateFavoriteStatus(true);

			// Assert
			Assert.That(_service.CurrentTrack.IsFavorite, Is.True);
		}

		[Test]
		public void SetAvailableTracks_SetsTracksList()
		{
			// Arrange
			var tracks = new List<PlayableTrack>
			{
				new() { Id = "1" },
				new() { Id = "2" }
			};

			// Act
			_service.SetAvailableTracks(tracks);

			_service.PlayPlaylist(new List<PlayableTrack> { new() { Id = "test" } });

			_service.PlayNext();

			// Assert 
			Assert.That(_service.CurrentTrack, Is.Not.Null);
		}

		[TearDown]
		public void TearDown()
		{
			_service = null;
		}
	}
}