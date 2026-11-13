
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
/// <summary>
/// A simple ITemplateLoader loading templates from a configured directory.
/// </summary>
public class MyIncludeFromDisk : ITemplateLoader
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private readonly string _templateRoot;

    public MyIncludeFromDisk(string templateRoot)
    {
        _templateRoot = Path.GetFullPath(templateRoot);

        if (!_templateRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            _templateRoot += Path.DirectorySeparatorChar;
        }
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        var templatePath = Path.GetFullPath(Path.Combine(_templateRoot, templateName));

        if (!templatePath.StartsWith(_templateRoot, PathComparison))
        {
            throw new ScriptRuntimeException(callerSpan, $"Include `{templateName}` is outside the template root.");
        }

        return templatePath;
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        // Template path was produced by the `GetPath` method above in case the Template has 
        // not been loaded yet
        return File.ReadAllText(templatePath);
    }
    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return new ValueTask<string>(File.ReadAllTextAsync(templatePath));
    }
}
