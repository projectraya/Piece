using Piece.DTOs;

namespace Piece.Services
{
	public class PlayerService
	{
		private PlayableTrack? _currentTrack;
		private List<PlayableTrack> _queue = new();
		private List<PlayableTrack> _allAvailableTracks = new();
		private List<PlayableTrack> _history = new();
		private int _currentIndex = 0;

		public event Action? OnChange;
		public event Action? OnTrackChanged;

		public PlayableTrack? CurrentTrack => _currentTrack;
		public List<PlayableTrack> Queue => _queue;
		public bool IsPlaying { get; private set; }

		// Add this method
		public void SetAvailableTracks(List<PlayableTrack> tracks)
		{
			_allAvailableTracks = tracks;
			Console.WriteLine($"[PlayerService] Set {tracks.Count} available tracks");
		}

		public void PlayTrack(PlayableTrack track)
		{
			Console.WriteLine($"[PlayerService] Playing: {track.Title}");

			// Add current track to history before switching
			if (_currentTrack != null && (_history.Count == 0 || _history.Last().Id != _currentTrack.Id))
			{
				_history.Add(_currentTrack);
			}

			_currentTrack = track;
			IsPlaying = true;

			// Add to queue if not already there
			if (!_queue.Any(t => t.Id == track.Id))
			{
				_queue.Add(track);
				_currentIndex = _queue.Count - 1;
			}
			else
			{
				_currentIndex = _queue.FindIndex(t => t.Id == track.Id);
			}

			NotifyTrackChanged();
			NotifyStateChanged();
		}

		public void PlayPlaylist(List<PlayableTrack> tracks, int startIndex = 0)
		{
			Console.WriteLine($"[PlayerService] Playing playlist: {tracks.Count} tracks");
			_queue = new List<PlayableTrack>(tracks);
			_currentIndex = startIndex;

			if (_queue.Any())
			{
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				NotifyTrackChanged();
				NotifyStateChanged();
			}
		}

		public void PlayNext()
		{
			// Add current to history
			if (_currentTrack != null)
			{
				_history.Add(_currentTrack);
			}

			// Pick a random track from ALL available tracks
			if (_allAvailableTracks.Any())
			{
				var random = new Random();
				var randomTrack = _allAvailableTracks[random.Next(_allAvailableTracks.Count)];

				_currentTrack = randomTrack;

				// Add to queue if not already there
				if (!_queue.Any(t => t.Id == randomTrack.Id))
				{
					_queue.Add(randomTrack);
				}
				_currentIndex = _queue.FindIndex(t => t.Id == randomTrack.Id);

				IsPlaying = true;
				NotifyTrackChanged();
				NotifyStateChanged();
				Console.WriteLine($"[PlayerService] Playing random track: {_currentTrack.Title}");
			}
			else if (_queue.Any())
			{
				// Fallback: pick from queue if no available tracks set
				var random = new Random();
				_currentIndex = random.Next(_queue.Count);
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				NotifyTrackChanged();
				NotifyStateChanged();
			}
		}

		public void PlayPrevious()
		{
			// Play the last track from history
			if (_history.Any())
			{
				var previousTrack = _history.Last();
				_history.RemoveAt(_history.Count - 1);

				_currentTrack = previousTrack;
				_currentIndex = _queue.FindIndex(t => t.Id == previousTrack.Id);
				if (_currentIndex == -1)
				{
					_queue.Add(previousTrack);
					_currentIndex = _queue.Count - 1;
				}

				IsPlaying = true;
				NotifyTrackChanged();
				NotifyStateChanged();
				Console.WriteLine($"[PlayerService] Playing previous track: {_currentTrack.Title}");
			}
			else
			{
				Console.WriteLine("[PlayerService] No previous track in history");
			}
		}

		public void TogglePlayPause()
		{
			IsPlaying = !IsPlaying;
			NotifyStateChanged();
		}

		public void UpdateFavoriteStatus(bool isFavorite)
		{
			if (_currentTrack != null)
			{
				_currentTrack.IsFavorite = isFavorite;
				NotifyStateChanged();
			}
		}

		private void NotifyStateChanged() => OnChange?.Invoke();
		private void NotifyTrackChanged() => OnTrackChanged?.Invoke();
	}
}