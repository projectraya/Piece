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

		public void SetAvailableTracks(List<PlayableTrack> tracks)
		{
			_allAvailableTracks = tracks;
			Console.WriteLine($"[PlayerService] Set {tracks.Count} available tracks");
		}

		public void PlayTrack(PlayableTrack track)
		{
			Console.WriteLine($"[PlayerService] Playing: {track.Title}");

			if (_currentTrack != null && (_history.Count == 0 || _history.Last().Id != _currentTrack.Id))
			{
				_history.Add(_currentTrack);
			}

			_currentTrack = track;
			IsPlaying = true;

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
			// Add current track to history
			if (_currentTrack != null && (_history.Count == 0 || _history.Last().Id != _currentTrack.Id))
			{
				_history.Add(_currentTrack);
			}

			// If we have tracks in queue, play the next one
			if (_queue.Any() && _currentIndex < _queue.Count - 1)
			{
				_currentIndex++;
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				Console.WriteLine($"[PlayerService] Playing next track from queue: {_currentTrack.Title}");
			}
			// If we're at the end of queue but have available tracks, play random
			else if (_allAvailableTracks.Any())
			{
				var random = new Random();
				var randomTrack = _allAvailableTracks[random.Next(_allAvailableTracks.Count)];
				_currentTrack = randomTrack;

				// Add to queue if not already there
				if (!_queue.Any(t => t.Id == randomTrack.Id))
				{
					_queue.Add(randomTrack);
					_currentIndex = _queue.Count - 1;
				}
				else
				{
					_currentIndex = _queue.FindIndex(t => t.Id == randomTrack.Id);
				}

				IsPlaying = true;
				Console.WriteLine($"[PlayerService] Playing random track: {_currentTrack.Title}");
			}
			// Otherwise loop back to start of queue
			else if (_queue.Any())
			{
				_currentIndex = 0;
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				Console.WriteLine($"[PlayerService] Looping back to start of queue: {_currentTrack.Title}");
			}

			NotifyTrackChanged();
			NotifyStateChanged();
		}

		public void PlayPrevious()
		{
			// If we have history, play the last track from history
			if (_history.Any())
			{
				var previousTrack = _history.Last();
				_history.RemoveAt(_history.Count - 1);

				_currentTrack = previousTrack;
				_currentIndex = _queue.FindIndex(t => t.Id == previousTrack.Id);

				// If track not in queue, add it
				if (_currentIndex == -1)
				{
					_queue.Insert(0, previousTrack); // Add at beginning
					_currentIndex = 0;
				}

				IsPlaying = true;
				Console.WriteLine($"[PlayerService] Playing previous track from history: {_currentTrack.Title}");
			}
			// If no history but we're not at the start of queue, go back in queue
			else if (_queue.Any() && _currentIndex > 0)
			{
				_currentIndex--;
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				Console.WriteLine($"[PlayerService] Playing previous track from queue: {_currentTrack.Title}");
			}
			// Otherwise, wrap to end of queue
			else if (_queue.Any())
			{
				_currentIndex = _queue.Count - 1;
				_currentTrack = _queue[_currentIndex];
				IsPlaying = true;
				Console.WriteLine($"[PlayerService] Wrapping to end of queue: {_currentTrack.Title}");
			}
			else
			{
				Console.WriteLine("[PlayerService] No previous track available");
			}

			NotifyTrackChanged();
			NotifyStateChanged();
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

		// QUEUE MANAGEMENT METHODS

		public void AddToQueue(PlayableTrack track)
		{
			// Always add to queue, even if it's a duplicate
			_queue.Add(track);
			Console.WriteLine($"[PlayerService] Added to queue: {track.Title} (Queue now has {_queue.Count} tracks)");
			NotifyStateChanged();
		}

		public void RemoveFromQueue(int index)
		{
			if (index >= 0 && index < _queue.Count)
			{
				var track = _queue[index];
				_queue.RemoveAt(index);

				// Adjust current index if needed
				if (index < _currentIndex)
				{
					_currentIndex--;
				}
				else if (index == _currentIndex && _currentTrack?.Id == track.Id)
				{
					// If we removed the current track, play next one or stop
					if (_queue.Any())
					{
						_currentIndex = Math.Min(_currentIndex, _queue.Count - 1);
						_currentTrack = _queue[_currentIndex];
						NotifyTrackChanged();
					}
					else
					{
						_currentTrack = null;
						IsPlaying = false;
						_currentIndex = 0;
						NotifyTrackChanged();
					}
				}

				Console.WriteLine($"[PlayerService] Removed from queue: {track.Title}");
				NotifyStateChanged();
			}
		}

		public void ReorderQueue(int fromIndex, int toIndex)
		{
			if (fromIndex >= 0 && fromIndex < _queue.Count &&
				toIndex >= 0 && toIndex < _queue.Count &&
				fromIndex != toIndex)
			{
				var track = _queue[fromIndex];
				_queue.RemoveAt(fromIndex);
				_queue.Insert(toIndex, track);

				// Update current index if needed
				if (_currentTrack != null)
				{
					_currentIndex = _queue.FindIndex(t => t.Id == _currentTrack.Id);
				}

				Console.WriteLine($"[PlayerService] Reordered queue: moved '{track.Title}' from {fromIndex} to {toIndex}");
				NotifyStateChanged();
			}
		}

		public void ClearQueue()
		{
			_queue.Clear();
			_currentTrack = null;
			_currentIndex = 0;
			_history.Clear();
			IsPlaying = false;
			Console.WriteLine("[PlayerService] Queue cleared");
			NotifyTrackChanged();
			NotifyStateChanged();
		}

		private void NotifyStateChanged() => OnChange?.Invoke();
		private void NotifyTrackChanged() => OnTrackChanged?.Invoke();
	}
}