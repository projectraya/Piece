using Microsoft.AspNetCore.Components;
using Piece.Services;

namespace Piece.Components
{
	public class LocalizedComponentBase : ComponentBase, IDisposable
	{
		[Inject] public LanguageService Lang { get; set; } = default!;

		protected override void OnInitialized()
		{
			Lang.OnLanguageChanged += StateHasChanged;
		}

		public virtual void Dispose()
		{
			Lang.OnLanguageChanged -= StateHasChanged;
		}
	}
}