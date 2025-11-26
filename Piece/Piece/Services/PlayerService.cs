using Piece.DTOs;

namespace Piece.Services
{
	public class PlayerService
	{
		private PlayableTrack? _currentTrack;
		private List<PlayableTrack> _queue = new();
		private int _currentIndex = 0;

		// Events to notify UI of changes
		public event Action? OnChange;
		public event Action? OnTrackChanged;

		// Public properties
		public PlayableTrack? CurrentTrack => _currentTrack;
		public List<PlayableTrack> Queue => _queue;
		public bool IsPlaying { get; private set; }

		// Play a single track
		public void PlayTrack(PlayableTrack track)
		{
			Console.WriteLine($"[PlayerService] Playing: {track.Title}");
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

			Console.WriteLine($"[PlayerService] About to notify track changed");
			NotifyTrackChanged();
			Console.WriteLine($"[PlayerService] About to notify state changed");
			NotifyStateChanged();
			Console.WriteLine($"[PlayerService] Notifications sent");
		}

		// Play a playlist/queue
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

		// Next track
		public void PlayNext()
		{
			if (_queue.Any() && _currentIndex < _queue.Count - 1)
			{
				_currentIndex++;
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				NotifyTrackChanged();
				NotifyStateChanged();
			}
		}

		// Previous track
		public void PlayPrevious()
		{
			if (_queue.Any() && _currentIndex > 0)
			{
				_currentIndex--;
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				NotifyTrackChanged();
				NotifyStateChanged();
			}
		}
		public void UpdateFavoriteStatus(bool isFavorite)
		{
			if(_currentTrack != null)
			{
				_currentTrack.IsFavorite = isFavorite;
				NotifyStateChanged();
			}
		}

		// Toggle play/pause
		public void TogglePlayPause()
		{
			IsPlaying = !IsPlaying;
			NotifyStateChanged();
		}

		private void NotifyStateChanged()
		{
			Console.WriteLine($"[PlayerService] NotifyStateChanged called, listeners: {OnChange?.GetInvocationList().Length ?? 0}");
			OnChange?.Invoke();
		}

		private void NotifyTrackChanged()
		{
			Console.WriteLine($"[PlayerService] NotifyTrackChanged called, listeners: {OnTrackChanged?.GetInvocationList().Length ?? 0}");
			OnTrackChanged?.Invoke();
		}
	}
}

