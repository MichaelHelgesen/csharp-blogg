using Markdig;

string outputPath = "../../output";
string sourcePath = "../../content/posts";

if (!Directory.Exists(sourcePath))
{
    Console.WriteLine($"Kildemappen ({sourcePath}) mangler eller er ikke tilgjengelig");
    return;
}

// Filene å gå gjennom. EnumerationOptions er "hvordan skal filene gjennomgås".
// Her så skal alle filer matche store og små bokstaver i "md".
var files = GetFiles(sourcePath, "*.md", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

try
{
    // Prøv å opprette katalog
    Directory.CreateDirectory(outputPath);
}
catch (UnauthorizedAccessException e)
{
    Console.WriteLine($"Ingen tilgang til å opprette {outputPath} - {e}");
    return;
}

// Iterere over filer
foreach (var file in files)
{
    try
    {
        var markdown = File.ReadAllText(file.FullName);
        var html = Markdown.ToHtml(markdown);
        File.WriteAllText(Path.Combine(outputPath, Path.ChangeExtension(file.Name, "html")), html);
        Console.WriteLine($"File {file.Name} generated");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error converting and creating file {file.FullName} - {ex}");
    }
}

FileInfo[] GetFiles(string path, string pattern, EnumerationOptions options)
{
    return new DirectoryInfo(path).GetFiles(pattern, options);
}



