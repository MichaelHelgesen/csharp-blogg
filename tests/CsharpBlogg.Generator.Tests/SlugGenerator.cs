namespace CsharpBlogg.Generator.Tests.Utilities;

using CsharpBlogg.Generator.Utilities;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Dette er   en tittel med mange mellomrom mellom   ord", "dette-er-en-tittel-med-mange-mellomrom-mellom-ord")]
    [InlineData("Enkel  test", "enkel-test")]
    [InlineData("Mange     mellomrom", "mange-mellomrom")]
    [InlineData("   Dette er en test   ", "dette-er-en-test")]
    [InlineData("Enkel test     ", "enkel-test")]
    [InlineData("   Mange     mellomrom", "mange-mellomrom")]
    public void MultipleSpacesBetweenWordsToOneDashAndTrim(string title, string expectedSlug)
    {
        // Act
        var slug = SlugGenerator.Slug(title);

        // Assert
        Assert.Equal(expectedSlug, slug);
    }

    [Fact]
    public void NorvegianLettersToEnglish()
    {
        // Arrange
        string title = "Dette er en tittel med øæå";

        // Act
        var slug = SlugGenerator.Slug(title);
        Assert.Equal("dette-er-en-tittel-med-oaeaa", slug);
    }

    [Fact]
    public void TrimDashesAndRemoveDoubleDashes()
    {
        // Arrange
        string title = "-dette--er en test -- ";

        // Act
        var slug = SlugGenerator.Slug(title);
        Assert.Equal("dette-er-en-test", slug);
    }
}
