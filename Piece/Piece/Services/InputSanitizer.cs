using System.Text.RegularExpressions;

namespace Piece.Services
{
	public interface IInputSanitizer
	{
		string SanitizeString(string input, int maxLength = 500);
		string SanitizeHtml(string input);
	}

	public class InputSanitizer : IInputSanitizer
	{
		private readonly IProfanityFilter _profanityFilter;

		public InputSanitizer(IProfanityFilter profanityFilter)
		{
			_profanityFilter = profanityFilter;
		}

		public string SanitizeString(string input, int maxLength = 500)
		{
			if (string.IsNullOrWhiteSpace(input))
				return string.Empty;

			input = Regex.Replace(input, "<.*?>", string.Empty);

			input = Regex.Replace(input, "<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

			input = input.Replace("<", "&lt;").Replace(">", "&gt;");

			input = _profanityFilter.FilterText(input);

			if (input.Length > maxLength)
				input = input.Substring(0, maxLength);

			return input.Trim();
		}

		public string SanitizeHtml(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return string.Empty;

			var result = Regex.Replace(input, "<.*?>", string.Empty);
			result = _profanityFilter.FilterText(result);

			return result;
		}
	}
}