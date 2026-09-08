using Ciir.Core.Documentation;
using Microsoft.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;

namespace Ciir.CSharp.Documentation;

/// <summary>Extracts formal XML documentation comments for a symbol.</summary>
internal static class XmlDocCommentExtractor
{
    public static CiirDocumentation? Extract(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml(expandIncludes: true);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        XElement root;
        try
        {
            root = XElement.Parse(xml);
        }
        catch (XmlException)
        {
            return null;
        }

        var summary = CleanText(root.Element("summary")?.Value);
        var remarks = CleanText(root.Element("remarks")?.Value);
        var returns = CleanText(root.Element("returns")?.Value);

        var parameters = root.Elements("param")
            .Select(element => new CiirDocumentationParameter
            {
                Name = element.Attribute("name")?.Value ?? string.Empty,
                Description = CleanText(element.Value) ?? string.Empty,
            })
            .Where(parameter => parameter.Name.Length > 0)
            .ToArray();

        var exceptions = root.Elements("exception")
            .Select(element => new CiirExceptionDocumentation
            {
                Type = StripCrefPrefix(element.Attribute("cref")?.Value),
                Description = CleanText(element.Value),
            })
            .Where(exception => exception.Type.Length > 0)
            .ToArray();

        if (summary is null && remarks is null && returns is null && parameters.Length == 0 && exceptions.Length == 0)
        {
            return null;
        }

        return new CiirDocumentation
        {
            Format = CiirDocumentationFormat.XmlDoc,
            Source = CiirDocumentationSource.Declared,
            Summary = summary,
            Remarks = remarks,
            Returns = returns,
            Parameters = parameters,
            Exceptions = exceptions,
        };
    }

    private static string StripCrefPrefix(string? cref)
    {
        if (string.IsNullOrEmpty(cref))
        {
            return string.Empty;
        }

        return cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
    }

    private static string? CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lines = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
        var cleaned = string.Join(' ', lines);
        return cleaned.Length > 0 ? cleaned : null;
    }
}
