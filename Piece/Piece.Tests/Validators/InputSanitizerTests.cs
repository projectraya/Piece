using NUnit.Framework;
using Moq;
using Piece.Services;

namespace Piece.Tests.Services
{
	[TestFixture]
	public class InputSanitizerTests
	{
		private Mock<IProfanityFilter> _mockProfanityFilter;
		private InputSanitizer _sanitizer;

		[SetUp]
		public void Setup()
		{
			_mockProfanityFilter = new Mock<IProfanityFilter>();
			_mockProfanityFilter
				.Setup(x => x.FilterText(It.IsAny<string>()))
				.Returns<string>(input => input);

			_sanitizer = new InputSanitizer(_mockProfanityFilter.Object);
		}

		[Test]
		public void SanitizeString_RemovesHtmlTags()
		{
			// Arrange
			var input = "<div>Hello</div> World";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result.Contains("<div>"), Is.False);
			Assert.That(result.Contains("</div>"), Is.False);
			Assert.That(result, Does.Contain("Hello"));
		}

		[Test]
		public void SanitizeString_RemovesScriptTags()
		{
			// Arrange
			var input = "<script>alert('xss')</script>Hello";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result.Contains("<script>"), Is.False);
			Assert.That(result.Contains("</script>"), Is.False);
			Assert.That(result, Does.Contain("Hello"));
		}

		[Test]
		public void SanitizeString_HandlesTextWithoutTags()
		{
			// Arrange
			var input = "Tom & Jerry";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result, Does.Contain("Tom & Jerry"));
			Assert.That(result.Contains("<"), Is.False);
			Assert.That(result.Contains(">"), Is.False);
		}

		[TestCase(null, "")]
		[TestCase("", "")]
		[TestCase("   ", "")]
		public void SanitizeString_HandlesNullOrEmpty(string input, string expected)
		{
			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void SanitizeString_TruncatesToMaxLength()
		{
			// Arrange
			var input = new string('a', 600);
			var maxLength = 500;

			// Act
			var result = _sanitizer.SanitizeString(input, maxLength);

			// Assert
			Assert.That(result.Length, Is.EqualTo(maxLength));
		}

		[Test]
		public void SanitizeString_TrimsWhitespace()
		{
			// Arrange
			var input = "  Hello World  ";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result, Is.EqualTo("Hello World"));
		}

		[Test]
		public void SanitizeString_CallsProfanityFilter()
		{
			// Arrange
			var input = "Some text";

			// Act
			_sanitizer.SanitizeString(input);

			// Assert
			_mockProfanityFilter.Verify(x => x.FilterText(It.IsAny<string>()), Times.Once);
		}

		[Test]
		public void SanitizeHtml_RemovesHtmlTags()
		{
			// Arrange
			var input = "<p>Hello <b>World</b></p>";

			// Act
			var result = _sanitizer.SanitizeHtml(input);

			// Assert
			Assert.That(result.Contains("<p>"), Is.False);
			Assert.That(result.Contains("<b>"), Is.False);
			Assert.That(result, Does.Contain("Hello"));
			Assert.That(result, Does.Contain("World"));
		}

		[Test]
		public void SanitizeHtml_CallsProfanityFilter()
		{
			// Arrange
			var input = "Some text";

			// Act
			_sanitizer.SanitizeHtml(input);

			// Assert
			_mockProfanityFilter.Verify(x => x.FilterText(It.IsAny<string>()), Times.Once);
		}

		[Test]
		public void SanitizeString_RemovesMultipleScriptTags()
		{
			// Arrange
			var input = "<script>bad1</script>Good<script>bad2</script>";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result.Contains("<script>"), Is.False);
			Assert.That(result, Does.Contain("Good"));
		}

		[Test]
		public void SanitizeString_HandlesNestedTags()
		{
			// Arrange
			var input = "<div><span>Content</span></div>";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result.Contains("<"), Is.False);
			Assert.That(result.Contains(">"), Is.False);
			Assert.That(result, Does.Contain("Content"));
		}

		[Test]
		public void SanitizeString_PreservesSpaces()
		{
			// Arrange
			var input = "Hello   World   Test";

			// Act
			var result = _sanitizer.SanitizeString(input);

			// Assert
			Assert.That(result, Does.Contain("Hello"));
			Assert.That(result, Does.Contain("World"));
			Assert.That(result, Does.Contain("Test"));
		}

		[TearDown]
		public void TearDown()
		{
			_sanitizer = null;
			_mockProfanityFilter = null;
		}
	}
}