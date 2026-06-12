using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace TypesSplitter.Core;

/// <summary>A loaded types.xml, with its &lt;type&gt; elements grouped by category.</summary>
public sealed class TypesDocument
{
    public const string FallbackCategory = "other";

    private readonly IReadOnlyDictionary<string, IReadOnlyList<XElement>> _byCategory;

    private TypesDocument(IReadOnlyDictionary<string, IReadOnlyList<XElement>> byCategory)
    {
        _byCategory = byCategory;
    }

    /// <summary>Category name → number of types in it, sorted by name.</summary>
    public IReadOnlyList<(string Category, int Count)> Categories =>
        _byCategory.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                   .Select(kv => (kv.Key, kv.Value.Count))
                   .ToList();

    public int TotalTypes => _byCategory.Values.Sum(v => v.Count);

    public static TypesDocument Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static TypesDocument Load(Stream stream)
    {
        // DTDs are not part of the types.xml format; prohibiting them blocks XXE.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
        using var reader = XmlReader.Create(stream, settings);
        var doc = XDocument.Load(reader);

        if (doc.Root is null || doc.Root.Name != "types")
            throw new FormatException("Not a types.xml: root element must be <types>.");

        var byCategory = new Dictionary<string, List<XElement>>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in doc.Root.Elements("type"))
        {
            var name = type.Element("category")?.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                name = FallbackCategory;
            if (!byCategory.TryGetValue(name, out var list))
                byCategory[name] = list = [];
            list.Add(type);
        }

        return new TypesDocument(byCategory.ToDictionary(
            kv => kv.Key, kv => (IReadOnlyList<XElement>)kv.Value, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Writes one types_&lt;category&gt;.xml per selected category into <paramref name="outputDirectory"/>.
    /// Returns category → (fileName, itemCount) for everything written.
    /// </summary>
    public IReadOnlyList<SplitResult> Split(
        string outputDirectory,
        IReadOnlyCollection<string>? selectedCategories = null,
        Action<string>? progress = null)
    {
        Directory.CreateDirectory(outputDirectory);

        var results = new List<SplitResult>();
        foreach (var (category, items) in _byCategory.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (selectedCategories is not null &&
                !selectedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
                continue;

            var fileName = $"types_{SanitizeFileName(category)}.xml";
            progress?.Invoke($"Writing {fileName} ({items.Count} types)…");

            var root = new XElement("types");
            foreach (var item in items)
                root.Add(item);

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                root);

            var path = Path.Combine(outputDirectory, fileName);
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            };
            using var writer = XmlWriter.Create(path, writerSettings);
            doc.Save(writer);

            results.Add(new SplitResult(category, fileName, items.Count));
        }

        return results;
    }

    /// <summary>
    /// The cfgeconomycore.xml &lt;ce&gt; block that registers the split files on a server.
    /// </summary>
    public static string CfgEconomyCoreSnippet(IEnumerable<SplitResult> results, string folder = "db")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<ce folder=\"{folder}\">");
        foreach (var r in results)
            sb.AppendLine($"    <file name=\"{r.FileName}\" type=\"types\" />");
        sb.Append("</ce>");
        return sb.ToString();
    }

    internal static string SanitizeFileName(string category) =>
        Regex.Replace(category, @"[^\w.-]", "_");
}

public sealed record SplitResult(string Category, string FileName, int Count);
