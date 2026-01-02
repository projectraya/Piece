namespace Piece.Services
{
	public interface IProfanityFilter
	{
		string FilterText(string text);
		bool ContainsProfanity(string text);
	}

	public class ProfanityFilter : IProfanityFilter
	{
		private readonly HashSet<string> _profanityList;
		private readonly char _replacementChar = '*';

		public ProfanityFilter()
		{
			// Common profanity list (add more as needed)
			_profanityList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"fuck", "shit", "ass", "bitch", "damn", "bastard", "crap",
				"dick", "piss", "pussy", "cock", "cunt", "whore", "slut",
				"fag", "nigger", "retard", "hell", "asshole", "motherfucker",
				"bullshit", "dumbass", "jackass", "prick", "douche", "twat", 
				"nigga", "n word", "nnn", "fat", "fk", "f@k", "fy", "niggers"
                
            };
		}

		public string FilterText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;

			var words = text.Split(' ');
			for (int i = 0; i < words.Length; i++)
			{
				var cleanWord = new string(words[i].Where(char.IsLetterOrDigit).ToArray());

				if (_profanityList.Contains(cleanWord))
				{
					// Replace with asterisks, keeping first letter
					if (words[i].Length > 0)
					{
						words[i] = words[i][0] + new string(_replacementChar, words[i].Length - 1);
					}
				}
			}

			return string.Join(" ", words);
		}

		public bool ContainsProfanity(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return false;

			var words = text.Split(' ');
			foreach (var word in words)
			{
				var cleanWord = new string(word.Where(char.IsLetterOrDigit).ToArray());
				if (_profanityList.Contains(cleanWord))
					return true;
			}

			return false;
		}
	}
}