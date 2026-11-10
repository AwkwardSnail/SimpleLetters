using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace SimpleLetters;

/// <summary>
/// A piece of text in one size and style, or a line break (Text is null).
/// Serif = Literata; otherwise the plain (sans) font, Atkinson Hyperlegible Next.
/// </summary>
public sealed record LetterRun(string? Text, double SizePt,
    bool Serif = false, bool Bold = false, bool Italic = false, bool Underline = false)
{
    /// <summary>True when the two pieces look the same (apart from their text).</summary>
    public bool SameLook(LetterRun other) =>
        SizePt == other.SizePt && Serif == other.Serif && Bold == other.Bold && Italic == other.Italic && Underline == other.Underline;
}

public sealed class LetterParagraph
{
    /// <summary>The paragraph's own size, which sets the height of an empty line.</summary>
    public double SizePt { get; set; } = 12;
    public List<LetterRun> Runs { get; } = new();
}

/// <summary>
/// A SimpleLetters document: the page margins and the text with its sizes and
/// styles. Saved as a small XML file with the .sletter extension. A run's
/// font, bold, italic and underline are only written when they're used:
/// <code>
/// &lt;sletter version="1"&gt;
///   &lt;page size="A4" units="mm" top="25" bottom="25" left="25" right="25"/&gt;
///   &lt;text&gt;
///     &lt;p size="12"&gt;&lt;run size="12"&gt;Dear Sam,&lt;/run&gt;&lt;/p&gt;
///     &lt;p size="12"/&gt;
///     &lt;p size="12"&gt;&lt;run size="12"&gt;Thanks for&lt;/run&gt;&lt;run size="16" font="serif" bold="true"&gt; everything&lt;/run&gt;&lt;br/&gt;&lt;/p&gt;
///   &lt;/text&gt;
/// &lt;/sletter&gt;
/// </code>
/// </summary>
public sealed class Letter
{
    public const string Extension = ".sletter";
    const int FormatVersion = 1;

    public double MarginTop { get; set; } = 25;
    public double MarginBottom { get; set; } = 25;
    public double MarginLeft { get; set; } = 25;
    public double MarginRight { get; set; } = 25;
    public List<LetterParagraph> Paragraphs { get; } = new();

    static double DipToPt(double dip) => Math.Round(dip * 72.0 / 96.0 * 2) / 2; // nearest half point
    static double PtToDip(double pt) => pt * 96.0 / 72.0;
    static double ClampSize(double pt) => Math.Clamp(pt, 1, 400);
    static double ClampMargin(double mm) => Math.Clamp(mm, 0, 150);

    // ------------------------------------------------------- FlowDocument

    /// <summary>Reads the text, sizes and styles out of the editor's document.</summary>
    public static Letter FromFlowDocument(FlowDocument document, FontFamily serif)
    {
        var letter = new Letter();
        foreach (var block in document.Blocks)
        {
            var paragraph = new LetterParagraph();
            if (block is Paragraph p)
            {
                paragraph.SizePt = DipToPt(p.FontSize);
                AddInlines(paragraph, p.Inlines, serif);
            }
            else
            {
                // Shouldn't happen (pasting is plain text only), but keep any text.
                paragraph.SizePt = DipToPt(block.FontSize);
                var text = new TextRange(block.ContentStart, block.ContentEnd).Text;
                if (text.Length > 0) paragraph.Runs.Add(new LetterRun(text, paragraph.SizePt));
            }
            letter.Paragraphs.Add(paragraph);
        }
        return letter;
    }

    static void AddInlines(LetterParagraph paragraph, InlineCollection inlines, FontFamily serif)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Run run when run.Text.Length > 0:
                    var piece = LookOf(run, serif) with { Text = run.Text };
                    // Merge neighbouring runs that look the same to keep the file tidy.
                    if (paragraph.Runs.Count > 0 && paragraph.Runs[^1] is { Text: { } prev } last && last.SameLook(piece))
                        paragraph.Runs[^1] = last with { Text = prev + run.Text };
                    else
                        paragraph.Runs.Add(piece);
                    break;
                case LineBreak lineBreak:
                    paragraph.Runs.Add(new LetterRun(null, DipToPt(lineBreak.FontSize)));
                    break;
                case Span span:
                    AddInlines(paragraph, span.Inlines, serif);
                    break;
            }
        }
    }

    /// <summary>The size and style of a piece of text (or of the paragraph at an empty line).</summary>
    public static LetterRun LookOf(TextElement element, FontFamily serif) => new(null, DipToPt(element.FontSize),
        Serif: element.FontFamily.Source == serif.Source,
        Bold: element.FontWeight.ToOpenTypeWeight() >= FontWeights.SemiBold.ToOpenTypeWeight(),
        Italic: element.FontStyle == FontStyles.Italic || element.FontStyle == FontStyles.Oblique,
        Underline: element is Inline inline && IsUnderlined(inline));

    /// <summary>True when the text, or a span around it, is underlined.</summary>
    public static bool IsUnderlined(Inline inline)
    {
        for (DependencyObject? e = inline; e is Inline i; e = i.Parent)
            if (i.TextDecorations?.Any(d => d.Location == TextDecorationLocation.Underline) == true)
                return true;
        return false;
    }

    /// <summary>Gives a piece of text in the editor (or for printing) the look of a run.</summary>
    public static void Style(Run run, LetterRun look, FontFamily sans, FontFamily serif)
    {
        run.FontSize = PtToDip(look.SizePt);
        run.FontFamily = look.Serif ? serif : sans;
        run.FontWeight = look.Bold ? FontWeights.Bold : FontWeights.Normal;
        run.FontStyle = look.Italic ? FontStyles.Italic : FontStyles.Normal;
        run.TextDecorations = look.Underline ? TextDecorations.Underline : null;
    }

    /// <summary>Builds a document for the editor (or for printing) from this letter.</summary>
    public FlowDocument ToFlowDocument(FontFamily sans, FontFamily serif, Brush ink, double defaultSizePt)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            FontFamily = sans,
            Foreground = ink,
            FontSize = PtToDip(Paragraphs.Count > 0 ? Paragraphs[0].SizePt : defaultSizePt),
            TextAlignment = TextAlignment.Left,
        };

        foreach (var lp in Paragraphs)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0), FontSize = PtToDip(lp.SizePt) };
            foreach (var run in lp.Runs)
            {
                if (run.Text is null)
                    paragraph.Inlines.Add(new LineBreak { FontSize = PtToDip(run.SizePt) });
                else
                {
                    var r = new Run(run.Text);
                    Style(r, run, sans, serif);
                    paragraph.Inlines.Add(r);
                }
            }
            document.Blocks.Add(paragraph);
        }

        if (document.Blocks.Count == 0)
            document.Blocks.Add(new Paragraph { Margin = new Thickness(0), FontSize = PtToDip(defaultSizePt) });
        return document;
    }

    /// <summary>Turns a plain text file into a letter, one paragraph per line.</summary>
    public static Letter FromPlainText(string text, double sizePt)
    {
        var letter = new Letter();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var paragraph = new LetterParagraph { SizePt = sizePt };
            if (line.Length > 0) paragraph.Runs.Add(new LetterRun(line, sizePt));
            letter.Paragraphs.Add(paragraph);
        }
        return letter;
    }

    // ---------------------------------------------------------------- XML

    public void Save(string path)
    {
        var xml = new XDocument(
            new XElement("sletter",
                new XAttribute("version", FormatVersion),
                new XElement("page",
                    new XAttribute("size", "A4"),
                    new XAttribute("units", "mm"),
                    new XAttribute("top", MarginTop),
                    new XAttribute("bottom", MarginBottom),
                    new XAttribute("left", MarginLeft),
                    new XAttribute("right", MarginRight)),
                new XElement("text",
                    Paragraphs.Select(p => new XElement("p",
                        new XAttribute("size", p.SizePt),
                        p.Runs.Select(r => r.Text is null
                            ? new XElement("br", new XAttribute("size", r.SizePt))
                            : new XElement("run",
                                new XAttribute("size", r.SizePt),
                                r.Serif ? new XAttribute("font", "serif") : null,
                                r.Bold ? new XAttribute("bold", true) : null,
                                r.Italic ? new XAttribute("italic", true) : null,
                                r.Underline ? new XAttribute("underline", true) : null,
                                XmlSafe(r.Text))))))));

        // Write to a temporary file first so a failed save never damages the old file.
        string temp = path + ".saving";
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (var writer = XmlWriter.Create(temp, settings))
            xml.Save(writer);
        File.Move(temp, path, overwrite: true);
    }

    public static Letter Load(string path)
    {
        var xml = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = xml.Root;
        if (root is null || root.Name != "sletter")
            throw new FormatException("This isn't a SimpleLetters document.");

        var letter = new Letter();
        if (root.Element("page") is { } page)
        {
            letter.MarginTop = ClampMargin((double?)page.Attribute("top") ?? 25);
            letter.MarginBottom = ClampMargin((double?)page.Attribute("bottom") ?? 25);
            letter.MarginLeft = ClampMargin((double?)page.Attribute("left") ?? 25);
            letter.MarginRight = ClampMargin((double?)page.Attribute("right") ?? 25);
        }

        foreach (var p in root.Element("text")?.Elements("p") ?? [])
        {
            var paragraph = new LetterParagraph { SizePt = ClampSize((double?)p.Attribute("size") ?? 12) };
            foreach (var e in p.Elements())
            {
                double size = ClampSize((double?)e.Attribute("size") ?? paragraph.SizePt);
                if (e.Name == "run")
                    paragraph.Runs.Add(new LetterRun(e.Value, size,
                        Serif: (string?)e.Attribute("font") == "serif",
                        Bold: (bool?)e.Attribute("bold") ?? false,
                        Italic: (bool?)e.Attribute("italic") ?? false,
                        Underline: (bool?)e.Attribute("underline") ?? false));
                else if (e.Name == "br") paragraph.Runs.Add(new LetterRun(null, size));
            }
            letter.Paragraphs.Add(paragraph);
        }
        return letter;
    }

    /// <summary>Drops the rare control characters XML can't hold.</summary>
    static string XmlSafe(string text)
    {
        var safe = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], c))
                safe.Append(c).Append(text[++i]); // emoji and other characters beyond U+FFFF
            else if (XmlConvert.IsXmlChar(c))
                safe.Append(c);
        }
        return safe.ToString();
    }
}
