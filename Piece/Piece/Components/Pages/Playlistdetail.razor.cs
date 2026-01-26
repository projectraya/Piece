using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Piece.Data.Enums;
using Piece.Data.Models;
using Piece.Data;
using Piece.Services;
using Microsoft.EntityFrameworkCore;
using Piece.DTOs;

namespace Piece.Components.Pages
{
	public partial class PlaylistDetail : ComponentBase, IDisposable
	{
		[Parameter] public int Id { get; set; }

		[Inject] private IPlaylistService PlaylistService { get; set; } = default!;
		[Inject] private PlayerService PlayerService { get; set; } = default!;
		[Inject] private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
		[Inject] private NavigationManager Navigation { get; set; } = default!;
		[Inject] private IJSRuntime JSRuntime { get; set; } = default!;
		[Inject] private IFavoriteService FavoriteService { get; set; } = default!;
		[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
		[Inject] private IWebHostEnvironment Environment { get; set; } = default!;

		public Playlist? playlist;
		public List<Track> playlistTracks = new();
		public List<Track> availableTracks = new();
		public Track? currentTrack;
		public bool isLoading = true;
		public string? currentUserId;
		public bool isAdmin = false;
		public bool isViewingOtherUsersPlaylist = false;

		// Add tracks modal
		public bool showAddTracksModal = false;
		public string searchQuery = "";

		// Cover image upload
		public InputFile? coverImageInput;

		// Toast notification
		public string? toastMessage;
		public string toastType = "error"; // "success" or "error"
		public bool showToast = false;

		public IEnumerable<Track> FilteredAvailableTracks =>
			string.IsNullOrWhiteSpace(searchQuery)
				? availableTracks
				: availableTracks.Where(t =>
					t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
					t.ArtistName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

		protected override async Task OnInitializedAsync()
		{
			var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
			var user = authState.User;

			if (user.Identity?.IsAuthenticated == true)
			{
				currentUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				await LoadPlaylist();
			}
			else
			{
				isLoading = false;
			}

			// Subscribe to PlayerService changes to update UI when track changes
			PlayerService.OnChange += OnPlayerStateChanged;
			PlayerService.OnTrackChanged += OnPlayerStateChanged;
		}

		private void OnPlayerStateChanged()
		{
			InvokeAsync(StateHasChanged);
		}

		public async Task LoadPlaylist()
		{

			if (string.IsNullOrEmpty(currentUserId))
			{
				isLoading = false;
				return;
			}
			using var context = await DbFactory.CreateDbContextAsync();
			isLoading = true;
			try
			{
				// Check if user is admin
				var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
				var isAdmin = authState.User.IsInRole("Admin");


				// Admins can view any playlist, regular users only their own
				if (isAdmin)
				{
					playlist = await context.Playlists
						.Include(p => p.PlaylistTracks)
							.ThenInclude(pt => pt.Track)
								.ThenInclude(t => t.Genre)
						.FirstOrDefaultAsync(p => p.Id == Id);
				}
				else
				{
					playlist = await PlaylistService.GetPlaylistByIdAsync(Id, currentUserId);
				}

				if (playlist != null)
				{
					isViewingOtherUsersPlaylist = isAdmin && playlist.UserId != currentUserId;
				}

				if (playlist != null)
				{
					if (isAdmin)
					{
						// For admin, load tracks directly from DbContext
						playlistTracks = await context.PlaylistTracks
							.Where(pt => pt.PlaylistId == Id)
							.Include(pt => pt.Track)
								.ThenInclude(t => t.Genre)
							.OrderBy(pt => pt.Position)
							.Select(pt => pt.Track)
							.ToListAsync();
					}
					else
					{
						playlistTracks = await PlaylistService.GetPlaylistTracksAsync(Id, currentUserId);
					}
				}

				// Load all available tracks from database
				availableTracks = await context.Tracks
					.Include(t => t.Genre)
					.Where(t => t.Source == TrackSource.Local && t.IsActive)
					.OrderBy(t => t.Title)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading playlist: {ex.Message}");
			}
			finally
			{
				isLoading = false;
			}
		}

		public void OpenAddTracksModal()
		{
			searchQuery = "";
			showAddTracksModal = true;
			StateHasChanged();
		}

		public void CloseAddTracksModal()
		{
			showAddTracksModal = false;
			StateHasChanged();
		}

		public async Task AddTrackToPlaylist(int trackId)
		{
			if (playlistTracks.Any(t => t.Id == trackId) || string.IsNullOrEmpty(currentUserId))
				return;

			try
			{
				var success = await PlaylistService.AddTrackToPlaylistAsync(Id, trackId, currentUserId);
				if (success)
				{
					await LoadPlaylist();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error adding track to playlist: {ex.Message}");
			}
		}

		public async Task RemoveTrackFromPlaylist(int trackId)
		{
			if (string.IsNullOrEmpty(currentUserId))
				return;

			try
			{
				var success = await PlaylistService.RemoveTrackFromPlaylistAsync(Id, trackId, currentUserId);
				if (success)
				{
					await LoadPlaylist();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error removing track from playlist: {ex.Message}");
			}
		}

		public async Task PlayTrack(Track track)
		{
			Console.WriteLine($"PlaylistDetail: Playing track {track.Title}");

			// Check if track is favorited
			bool isFavorite = false;
			if (!string.IsNullOrEmpty(currentUserId))
			{
				isFavorite = await FavoriteService.IsFavoriteAsync(currentUserId, track.Id);
			}

			var playableTrack = PlayableTrack.FromLocalTrack(track, isFavorite);
			PlayerService.PlayTrack(playableTrack);
			track.PlayCount++;
			StateHasChanged();
		}

		public void PlayPlaylist()
		{
			if (playlistTracks.Any())
			{
				var playableTracks = playlistTracks
					.Select(t => PlayableTrack.FromLocalTrack(t))
					.ToList();

				// If shuffle is on, randomize the tracks
				if (PlayerService.IsShuffleOn)
				{
					var random = new Random();
					playableTracks = playableTracks.OrderBy(_ => random.Next()).ToList();
				}

				PlayerService.PlayPlaylist(playableTracks, 0);
			}
		}

		public void ToggleShuffle()
		{
			PlayerService.ToggleShuffle();
		}

		public async Task OpenCoverUpload()
		{
			if (isViewingOtherUsersPlaylist)
				return;

			// Trigger the hidden file input click using a more reliable method
			try
			{
				await JSRuntime.InvokeVoidAsync("eval", "document.querySelector('.hidden-file-input').click()");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error opening file picker: {ex.Message}");
			}
		}

		public async Task HandleCoverImageUpload(InputFileChangeEventArgs e)
		{
			if (playlist == null || string.IsNullOrEmpty(currentUserId) || isViewingOtherUsersPlaylist)
			{
				return;
			}

			var file = e.File;
			if (file == null)
			{
				return;
			}

			try
			{
				// Validate file type
				var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
				if (!allowedTypes.Contains(file.ContentType.ToLower()))
				{
					ShowToast("Invalid file type. Please upload a JPEG, PNG, or WebP image.", "error");
					return;
				}

				// Validate file size (max 5MB)
				if (file.Size > 5 * 1024 * 1024)
				{
					ShowToast("File size too large. Maximum size is 5MB.", "error");
					return;
				}

				// Create uploads directory if it doesn't exist
				var uploadsPath = Path.Combine(Environment.WebRootPath, "uploads", "playlists");
				Directory.CreateDirectory(uploadsPath);

				// Generate unique filename
				var extension = Path.GetExtension(file.Name);
				var fileName = $"playlist_{Id}_{Guid.NewGuid()}{extension}";
				var filePath = Path.Combine(uploadsPath, fileName);

				// Save the file
				using (var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024))
				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await stream.CopyToAsync(fileStream);
				}

				// Update playlist cover URL in database
				using var context = await DbFactory.CreateDbContextAsync();
				var playlistToUpdate = await context.Playlists.FindAsync(Id);
				if (playlistToUpdate != null && playlistToUpdate.UserId == currentUserId)
				{
					// Delete old cover image if it exists
					if (!string.IsNullOrEmpty(playlistToUpdate.CoverImageUrl))
					{
						var oldFilePath = Path.Combine(Environment.WebRootPath, playlistToUpdate.CoverImageUrl.TrimStart('/'));
						if (File.Exists(oldFilePath))
						{
							File.Delete(oldFilePath);
						}
					}

					playlistToUpdate.CoverImageUrl = $"/uploads/playlists/{fileName}";
					await context.SaveChangesAsync();

					// Reload playlist to show new cover
					await LoadPlaylist();
					ShowToast("Cover image updated successfully!", "success");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error uploading cover image: {ex.Message}");
				ShowToast("Failed to upload cover image. Please try again.", "error");
			}
		}

		private async void ShowToast(string message, string type)
		{
			toastMessage = message;
			toastType = type;
			showToast = true;
			StateHasChanged();

			// Hide toast after 3 seconds
			await Task.Delay(3000);
			showToast = false;
			StateHasChanged();
		}

		public void Dispose()
		{
			PlayerService.OnChange -= OnPlayerStateChanged;
			PlayerService.OnTrackChanged -= OnPlayerStateChanged;
		}
	}
}