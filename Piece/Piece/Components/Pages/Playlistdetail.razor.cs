using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Piece.Data.Enums;
using Piece.Data.Models;
using Piece.Data;
using Piece.Services;
using Microsoft.EntityFrameworkCore;
using Piece.DTOs;

namespace Piece.Components.Pages
{
	public partial class PlaylistDetail : ComponentBase
	{
		[Parameter] public int Id { get; set; }

		[Inject] private IPlaylistService PlaylistService { get; set; } = default!;
		[Inject] private PlayerService PlayerService { get; set; } = default!;
		[Inject] private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
		[Inject] private NavigationManager Navigation { get; set; } = default!;
		[Inject] private IJSRuntime JSRuntime { get; set; } = default!;
		[Inject] private IFavoriteService FavoriteService { get; set; } = default!;
		[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

		public Playlist? playlist;
		public List<Track> playlistTracks = new();
		public List<Track> availableTracks = new();
		public Track? currentTrack;
		public bool isLoading = true;
		public string? currentUserId;

		// Add tracks modal
		public bool showAddTracksModal = false;
		public string searchQuery = "";

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
				PlayerService.PlayPlaylist(playableTracks, 0);
			}
		}
	}
}
