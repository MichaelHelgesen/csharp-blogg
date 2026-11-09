using System.Text;
using System.Text.RegularExpressions;
namespace CsharpBlogg.Generator.Utilities;

public static class Slug
{
    public static string SlugConverter(string title)
    {
        string titleCopy = title
                .ToLowerInvariant()
                // .Trim()
                .Replace("ø", "o").Replace("å", "aa").Replace("æ", "ae");

        titleCopy = titleCopy.Normalize(NormalizationForm.FormD);
        char[] arr = titleCopy.ToCharArray();

        arr = Array.FindAll(arr, c => char.IsAsciiLetterOrDigit(c)
                                          /* || char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark - feil*/
                                          || char.IsWhiteSpace(c)
                                          || c == '-');
        titleCopy = new string(arr);
        titleCopy = Regex.Replace(titleCopy, @"[\s-]+", "-");
        //test = Regex.Replace(test, @"[-]+", "-");
        titleCopy = titleCopy.Trim('-'); // kan ikke bruke ", for det er ikke char.
                                         //titleCopy = titleCopy.TrimStart('-'); // Trim tar begge ender, trenger ikke start og end
        titleCopy += ".html";
        return titleCopy;
    }
}
