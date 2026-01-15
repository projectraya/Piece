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
			_profanityList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"fuck", "shit", "ass", "bitch", "damn", "bastard", "crap",
				"dick", "piss", "pussy", "cock", "cunt", "whore", "slut",
				"fag", "nigger", "retard", "hell", "asshole", "motherfucker",
				"bullshit", "dumbass", "jackass", "prick", "douche", "twat",
				"nigga", "n word", "nnn", "fat", "fk", "f@k", "fy", "niggers",
				"niggas", "nigas", "nigs", "nnnngs", "ngs", "nig", "nigg@", "nigg@s",
				"yo mom", "your mom", "licky dicky", "mf", "mfs", "niga", "negro",
				"n1gg@", "n1g@", "negri", "negronis", "giganiga", "niglet", "nigglet", 
				"bikestealer", "ni ga", "ni gga", "ne gro", "ne ggro", "negrofication", 
				"negromacy", "niggervile", "cool aid brothers", "nizza", "jew", "juice",
				"negromania", "negrocity", "negger", "negur", "negri", "cheren", "idiot",
				"piknq", "kurwa", "kurva", "hui", "pishka", "tits", "boobs", "cici", "evrei",
				"evrein", "sera", "laino", "kreten", "guz", "guza ti", "maika ti", "maika", "sopol",
				"gadnqr", "retard", "pederas", "pederast", "pedo", "pedofil", "pedofile", "negroo", "negrooo", "negroooo"

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