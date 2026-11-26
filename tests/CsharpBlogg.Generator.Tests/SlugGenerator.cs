namespace CsharpBlogg.Generator.Tests.Utilities;
using CsharpBlogg.Generator.Utilities;

public class SlugGeneratorTests
{
    [Fact]
    public void MultipleSpacesBetweenWordsToOneDash()
    {
        // Arrange
        string title = "Dette er   en tittel med mange mellomrom mellom   ord";
        // Act
        var slug = SlugGenerator.Slug(title);
        Assert.Equal("dette-er-en-tittel-med-mange-mellomrom-mellom-ord", slug);
    }
}
