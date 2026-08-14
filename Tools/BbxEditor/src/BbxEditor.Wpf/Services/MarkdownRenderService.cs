using System.Net;
using System.Text;
using System.IO;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace BbxEditor.Wpf.Services;

public static class MarkdownRenderService
{
    private const string DesignPlanLinkHost = "bbx-design-plan.local";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .UseSmartyPants()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public static string RenderDocument(string markdown, string documentPath)
    {
        var syntax = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        foreach (var link in syntax.Descendants<LinkInline>().Where(link => !string.IsNullOrWhiteSpace(link.Url)))
        {
            link.Url = link.IsImage
                ? ResolveImageUrl(documentPath, link.Url!)
                : ResolveDocumentLinkUrl(documentPath, link.Url!);
        }

        var body = Markdown.ToHtml(syntax, Pipeline);
        var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(documentPath)) ?? AppContext.BaseDirectory;
        var baseUri = new Uri(Path.EndsInDirectorySeparator(documentDirectory)
            ? documentDirectory
            : documentDirectory + Path.DirectorySeparatorChar);

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <meta http-equiv="X-UA-Compatible" content="IE=edge">
              <base href="{{WebUtility.HtmlEncode(baseUri.AbsoluteUri)}}">
              <style>
                :root { color-scheme: dark; }
                html { background: #232528; }
                html, body { scrollbar-face-color: #626972; scrollbar-track-color: #232528; scrollbar-arrow-color: #f0f1f3; scrollbar-shadow-color: #232528; }
                body { max-width: 1080px; margin: 0 auto; padding: 32px 42px 64px; color: #f0f1f3; background: #292c30; font-family: "Segoe UI", "Microsoft YaHei UI", sans-serif; font-size: 15px; line-height: 1.65; overflow-wrap: break-word; }
                ::selection { color: #f0f1f3; background: #4a5d6d; }
                h1, h2, h3, h4, h5, h6 { color: #f0f1f3; line-height: 1.3; margin: 1.45em 0 .65em; }
                h1 { font-size: 2em; border-bottom: 1px solid #4a4f56; padding-bottom: .32em; }
                h2 { font-size: 1.55em; border-bottom: 1px solid #4a4f56; padding-bottom: .28em; }
                h3 { font-size: 1.25em; }
                p, ul, ol, blockquote, pre, table { margin: 0 0 1em; }
                a { color: #95a9bb; text-decoration: none; }
                a:hover { text-decoration: underline; }
                img { display: block; max-width: 100%; height: auto; margin: 16px auto; border: 1px solid #4a4f56; border-radius: 4px; }
                table { border-collapse: collapse; width: 100%; display: table; }
                th, td { border: 1px solid #4a4f56; padding: 7px 10px; text-align: left; vertical-align: top; }
                th { background: #383d44; font-weight: 600; }
                tr:nth-child(even) td { background: #30343a; }
                blockquote { margin-left: 0; padding: 4px 16px; color: #b6bbc2; border-left: 4px solid #8397aa; background: #303338; }
                code { padding: 2px 5px; border-radius: 3px; color: #f0f1f3; background: #202327; font-family: Consolas, "Cascadia Mono", monospace; font-size: .92em; }
                pre { padding: 14px 16px; overflow: auto; border: 1px solid #4a4f56; border-radius: 5px; background: #202327; }
                pre code { padding: 0; background: transparent; }
                hr { height: 1px; margin: 24px 0; border: 0; background: #4a4f56; }
                input[type=checkbox] { margin-right: 6px; }
                del { color: #858b94; }
              </style>
            </head>
            <body>{{body}}</body>
            </html>
            """;
    }

    public static string ResolveImageUrl(string documentPath, string imageUrl)
    {
        var value = WebUtility.HtmlDecode(imageUrl).Trim();
        if (value.Length == 0 || value.StartsWith('#') || value.StartsWith("//", StringComparison.Ordinal))
        {
            return value;
        }

        var decodedPath = Uri.UnescapeDataString(value).Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathFullyQualified(decodedPath)) return new Uri(Path.GetFullPath(decodedPath)).AbsoluteUri;
        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri) && absoluteUri.Scheme.Length > 1)
        {
            return value;
        }

        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(documentPath)) ?? AppContext.BaseDirectory;
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, decodedPath));
        return new Uri(fullPath).AbsoluteUri;
    }

    public static string ResolveDocumentLinkUrl(string documentPath, string linkUrl)
    {
        var value = WebUtility.HtmlDecode(linkUrl).Trim();
        if (value.Length == 0 || value.StartsWith('#') || value.StartsWith("//", StringComparison.Ordinal))
            return value;

        var fragmentIndex = value.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? value[(fragmentIndex + 1)..] : string.Empty;
        var targetValue = fragmentIndex >= 0 ? value[..fragmentIndex] : value;

        Uri targetUri;
        if (Uri.TryCreate(targetValue, UriKind.Absolute, out var absoluteUri) && absoluteUri.Scheme.Length > 1)
        {
            if (!absoluteUri.IsFile) return value;
            targetUri = absoluteUri;
        }
        else
        {
            var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(documentPath)) ?? AppContext.BaseDirectory;
            var baseUri = new Uri(Path.EndsInDirectorySeparator(documentDirectory)
                ? documentDirectory
                : documentDirectory + Path.DirectorySeparatorChar);
            if (!Uri.TryCreate(baseUri, targetValue, out var resolvedUri) || !resolvedUri.IsFile) return value;
            targetUri = resolvedUri;
        }

        if (!Path.GetExtension(targetUri.LocalPath).Equals(".md", StringComparison.OrdinalIgnoreCase)) return value;
        if (fragment.Length > 0) targetUri = new UriBuilder(targetUri) { Fragment = fragment }.Uri;
        return $"https://{DesignPlanLinkHost}/open?target={Uri.EscapeDataString(targetUri.AbsoluteUri)}";
    }

    public static bool TryDecodeDesignPlanLink(Uri navigationUri, out Uri? targetUri)
    {
        targetUri = null;
        if (!navigationUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !navigationUri.Host.Equals(DesignPlanLinkHost, StringComparison.OrdinalIgnoreCase) ||
            !navigationUri.AbsolutePath.Equals("/open", StringComparison.OrdinalIgnoreCase))
            return false;

        var targetValue = navigationUri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .FirstOrDefault(part => part.Length == 2 && part[0].Equals("target", StringComparison.OrdinalIgnoreCase));
        return targetValue is not null &&
               Uri.TryCreate(Uri.UnescapeDataString(targetValue[1]), UriKind.Absolute, out targetUri) &&
               targetUri.IsFile;
    }
}
