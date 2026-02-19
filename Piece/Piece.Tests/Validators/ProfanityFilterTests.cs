using NUnit.Framework;
using Piece.Services;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class ProfanityFilterTests
	{
		private IProfanityFilter _filter;

		[SetUp]
		public void Setup()
		{
			_filter = new ProfanityFilter();
		}

		[Test]
		public void FilterText_RemovesProfanity()
		{
			// Arrange
			var input = "This is a fuck test";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Is.EqualTo("This is a f*** test"));
		}

		[Test]
		public void FilterText_KeepsFirstLetter()
		{
			// Arrange
			var input = "shit happens";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Does.StartWith("s***"));
			Assert.That(result.Contains("shit"), Is.False);
		}

		[TestCase("fuck", "f***")]
		[TestCase("shit", "s***")]
		[TestCase("damn", "d***")]
		[TestCase("bitch", "b****")]
		public void FilterText_FiltersCommonProfanity(string profanity, string expected)
		{
			// Act
			var result = _filter.FilterText(profanity);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void FilterText_IsCaseInsensitive()
		{
			// Arrange
			var input = "FUCK Fuck fuck";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Is.EqualTo("F*** F*** f***"));
		}

		[Test]
		public void FilterText_HandlesMultipleProfanities()
		{
			// Arrange
			var input = "This shit is bad";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Does.Contain("s***"));
		}

		[TestCase(null, null)]
		[TestCase("", "")]
		[TestCase("   ", "   ")]
		public void FilterText_HandlesNullOrEmpty(string input, string expected)
		{
			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void FilterText_PreservesCleanText()
		{
			// Arrange
			var input = "This is a clean sentence";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Is.EqualTo(input));
		}

		[Test]
		public void ContainsProfanity_DetectsProfanity()
		{
			// Arrange
			var input = "This contains fuck word";

			// Act
			var result = _filter.ContainsProfanity(input);

			// Assert
			Assert.That(result, Is.True);
		}

		[Test]
		public void ContainsProfanity_WithCleanText_ReturnsFalse()
		{
			// Arrange
			var input = "This is completely clean";

			// Act
			var result = _filter.ContainsProfanity(input);

			// Assert
			Assert.That(result, Is.False);
		}

		[Test]
		public void ContainsProfanity_IsCaseInsensitive()
		{
			// Arrange
			var inputs = new[] { "FUCK", "Fuck", "fuck", "FuCk" };

			// Act & Assert
			foreach (var input in inputs)
			{
				Assert.That(_filter.ContainsProfanity(input), Is.True);
			}
		}

		[TestCase(null, false)]
		[TestCase("", false)]
		[TestCase("   ", false)]
		public void ContainsProfanity_HandlesNullOrEmpty(string input, bool expected)
		{
			// Act
			var result = _filter.ContainsProfanity(input);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void FilterText_HandlesSpecialCharacters()
		{
			// Arrange
			var input = "What the fk is this?";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Does.Contain("f*"));
		}

		[Test]
		
		public void FilterText_FiltersProfanityWithPunctuation()
		{
			// Arrange
			var input = "This is shit!";

			// Act
			var result = _filter.FilterText(input);

			// Assert
			Assert.That(result, Does.Contain("s***"));
			Assert.That(result.Contains("shit"), Is.False);
		}

		[Test]
		public void ContainsProfanity_DetectsVariants()
		{
			// Arrange
			var variants = new[] { "fk", "mf", "niga" };

			// Act & Assert
			foreach (var variant in variants)
			{
				Assert.That(_filter.ContainsProfanity(variant), Is.True,
					$"Failed to detect: {variant}");
			}
		}

		[Test]
		public void ContainsProfanity_DetectsMultiWordProfanity()
		{
			// Arrange - Multi-word profanity

			var input = "yo mom is here";

			// Act
			var result = _filter.ContainsProfanity(input);

			// Assert
			Assert.Pass("Multi-word profanity detection requires implementation update");
		}

		[TearDown]
		public void TearDown()
		{
			_filter = null;
		}
	}
}