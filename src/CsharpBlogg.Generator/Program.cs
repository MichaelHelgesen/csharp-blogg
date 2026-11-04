using Markdig;

var files = GetFiles("../../content/posts/*.md");

// Iterere over filer
foreach (var file in files)
{
    var reader = ReadAllText(file.FullName);
    var html = MarkdownToHTML(reader);
    
    Console.WriteLine(html);
}

static string ReadAllText(string file)
{
    return File.ReadAllText(file);
}


static string MarkdownToHTML(string file)
{
    var html = Markdown.ToHtml(file);
    return html;
}

FileInfo[] GetFiles(string pattern)
{
    return new DirectoryInfo(".").GetFiles(pattern);
}



