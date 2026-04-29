using Microsoft.JSInterop;

namespace Piece.Services
{
	public class NeonBeatInterop
	{
		private readonly IJSRuntime _js;

		public NeonBeatInterop(IJSRuntime js) => _js = js;

		public async Task LoadSong(string title, int bpm, byte[] audioBytes)
		{
			await _js.InvokeVoidAsync(
				"NeonBeatBridge.loadSongFromBytes",
				title,
				bpm,
				audioBytes
			);
		}
	}
}