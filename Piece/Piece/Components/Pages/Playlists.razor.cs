using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Piece.Data.Models;
using Piece.Services;

namespace Piece.Components.Pages
{
	public partial class Playlists : ComponentBase
	{
		[Inject] private IPlaylistService PlaylistService { get; set; } = default!;
		[Inject] private NavigationManager Navigation { get; set; } = default!;
		[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

		private List<Playlist> playlists = new();
		private bool isLoading = true;
		private string? currentUserId;

		// Modal state
		private bool showModal = false;
		private bool isEditMode = false;
		private int? editingPlaylistId = null;
		private string playlistName = "";
		private string playlistDescription = "";
		private bool isPublic = true;

		protected override async Task OnInitializedAsync()
		{
			var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
			var user = authState.User;

			if (user.Identity?.IsAuthenticated == true)
			{
				currentUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
				await LoadPlaylists();
			}
			else
			{
				isLoading = false;
			}
		}

		private async Task LoadPlaylists()
		{
			if (string.IsNullOrEmpty(currentUserId))
			{
				isLoading = false;
				return;
			}

			isLoading = true;
			try
			{
				playlists = await PlaylistService.GetUserPlaylistsAsync(currentUserId);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading playlists: {ex.Message}");
			}
			finally
			{
				isLoading = false;
			}
		}

		private void OpenCreateModal()
		{
			Console.WriteLine("OpenCreateModal called");
			isEditMode = false;
			editingPlaylistId = null;
			playlistName = "";
			playlistDescription = "";
			isPublic = true;
			showModal = true;
			Console.WriteLine($"showModal set to: {showModal}");
			StateHasChanged();
		}

		private void OpenEditModal(Playlist playlist)
		{
			isEditMode = true;
			editingPlaylistId = playlist.Id;
			playlistName = playlist.Name;
			playlistDescription = playlist.Description ?? "";
			isPublic = playlist.IsPublic;
			showModal = true;
			StateHasChanged();
		}

		private void CloseModal()
		{
			showModal = false;
			playlistName = "";
			playlistDescription = "";
			editingPlaylistId = null;
			isEditMode = false;
			StateHasChanged();
			Console.WriteLine("Modal closed");
		}

		private async Task SavePlaylist()
		{
			if (string.IsNullOrWhiteSpace(playlistName) || string.IsNullOrEmpty(currentUserId))
				return;

			try
			{
				if (isEditMode && editingPlaylistId.HasValue)
				{
					await PlaylistService.UpdatePlaylistAsync(
						editingPlaylistId.Value,
						currentUserId,
						playlistName.Trim(),
						string.IsNullOrWhiteSpace(playlistDescription) ? null : playlistDescription.Trim(),
						isPublic
					);
				}
				else
				{
					await PlaylistService.CreatePlaylistAsync(
						currentUserId,
						playlistName.Trim(),
						string.IsNullOrWhiteSpace(playlistDescription) ? null : playlistDescription.Trim(),
						isPublic
					);
				}

				await LoadPlaylists();
				CloseModal();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error saving playlist: {ex.Message}");
			}
		}

		private async Task DeletePlaylist(int id)
		{
			if (string.IsNullOrEmpty(currentUserId))
				return;

			try
			{
				await PlaylistService.DeletePlaylistAsync(id, currentUserId);
				await LoadPlaylists();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error deleting playlist: {ex.Message}");
			}
		}

		private void ViewPlaylist(int id)
		{
			Navigation.NavigateTo($"/playlist/{id}");
		}
	}
}
