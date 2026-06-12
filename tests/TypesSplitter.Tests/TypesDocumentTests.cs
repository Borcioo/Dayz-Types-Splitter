using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using TypesSplitter.Core;
using Xunit;

namespace TypesSplitter.Tests;

public class TypesDocumentTests
{
    private static TypesDocument LoadXml(string xml) =>
        TypesDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

    private const string Sample = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <types>
            <type name="AKM">
                <nominal>5</nominal>
                <lifetime>28800</lifetime>
                <category name="weapons"/>
                <usage name="Military"/>
            </type>
            <type name="Apple">
                <nominal>40</nominal>
                <category name="food"/>
            </type>
            <type name="Pear">
                <nominal>20</nominal>
                <category name="food"/>
            </type>
            <type name="ZmbM_CitizenASkinny">
                <nominal>0</nominal>
            </type>
            <type name="WeirdThing">
                <nominal>1</nominal>
                <category name=""/>
            </type>
        </types>
        """;

    [Fact]
    public void Load_GroupsTypesByCategory()
    {
        var doc = LoadXml(Sample);

        doc.TotalTypes.Should().Be(5);
        doc.Categories.Should().BeEquivalentTo(new[]
        {
            ("food", 2),
            ("other", 2),     // no <category> + empty name both fall back
            ("weapons", 1),
        });
    }

    [Fact]
    public void Load_RejectsNonTypesRoot()
    {
        var act = () => LoadXml("<economy></economy>");
        act.Should().Throw<FormatException>().WithMessage("*<types>*");
    }

    [Fact]
    public void Load_ProhibitsDtd()
    {
        var act = () => LoadXml("""<!DOCTYPE types [<!ENTITY x "y">]><types/>""");
        act.Should().Throw<System.Xml.XmlException>();
    }

    [Fact]
    public void Split_WritesOneFilePerCategory_AndPreservesElements()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ts-tests", Path.GetRandomFileName());
        try
        {
            var doc = LoadXml(Sample);
            var results = doc.Split(dir);

            results.Select(r => r.FileName).Should().BeEquivalentTo(
                "types_food.xml", "types_other.xml", "types_weapons.xml");
            results.Sum(r => r.Count).Should().Be(5);

            var weapons = XDocument.Load(Path.Combine(dir, "types_weapons.xml"));
            var akm = weapons.Root!.Elements("type").Single();
            akm.Attribute("name")!.Value.Should().Be("AKM");
            akm.Element("usage")!.Attribute("name")!.Value.Should().Be("Military");
            akm.Element("category")!.Attribute("name")!.Value.Should().Be("weapons");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Split_HonorsCategorySelection()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ts-tests", Path.GetRandomFileName());
        try
        {
            var doc = LoadXml(Sample);
            var results = doc.Split(dir, ["food"]);

            results.Should().ContainSingle().Which.Category.Should().Be("food");
            File.Exists(Path.Combine(dir, "types_food.xml")).Should().BeTrue();
            File.Exists(Path.Combine(dir, "types_weapons.xml")).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [InlineData("weapons", "weapons")]
    [InlineData("my stuff", "my_stuff")]
    [InlineData("a/b\\c:d", "a_b_c_d")]
    [InlineData("mod-1.2", "mod-1.2")]
    public void SanitizeFileName_StripsUnsafeCharacters(string input, string expected)
    {
        TypesDocument.SanitizeFileName(input).Should().Be(expected);
    }

    [Fact]
    public void CfgEconomyCoreSnippet_ListsAllFiles()
    {
        var snippet = TypesDocument.CfgEconomyCoreSnippet(
        [
            new SplitResult("food", "types_food.xml", 2),
            new SplitResult("weapons", "types_weapons.xml", 1),
        ]);

        snippet.Should().Be("""
            <ce folder="db">
                <file name="types_food.xml" type="types" />
                <file name="types_weapons.xml" type="types" />
            </ce>
            """.ReplaceLineEndings(Environment.NewLine));
    }
}
