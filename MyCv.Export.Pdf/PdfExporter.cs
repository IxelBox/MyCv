using Microsoft.Extensions.FileProviders;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Visitors;
using MigraDoc.Rendering;
using MyCv.Model;
using MyCv.Model.Providers;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Drawing;
using PdfSharp.Snippets.Font;

namespace MyCv.Export.Pdf;

internal static class Constants
{
    internal static class Styles
    {
        public const string Normal = nameof(Constants.Styles.Normal);
        public const string Italic = nameof(Constants.Styles.Italic);
        public const string Bold = nameof(Constants.Styles.Bold);
        public const string Heading1 = nameof(Constants.Styles.Heading1);
        public const string Heading2 = nameof(Constants.Styles.Heading2);
        public const string Heading3 = nameof(Constants.Styles.Heading3);
        public const string BulletList = nameof(Constants.Styles.BulletList);
    }
}

public class PdfExporter : IFileExporter
{
    private readonly IWorkingDirectoryProvider _folderProvider;

    public PdfExporter(IWorkingDirectoryProvider workingDirectoryProvider)
    {
        _folderProvider = workingDirectoryProvider;
        // NET6FIX
        if (PdfSharp.Capabilities.Build.IsCoreBuild)
            GlobalFontSettings.FontResolver = new FailsafeFontResolver();
    }
    
    public Task Create(SideStructure data, Stream stream)
    {
        var document = PdfDocument(data);

        var renderer = new PdfDocumentRenderer()
        {
            Document = document
        };

        renderer.RenderDocument();
        renderer.PdfDocument.Save(stream);

        return Task.CompletedTask;
    }

    private Document PdfDocument(SideStructure data)
    {
        bool showBorder = false;
        Document document = new()
        {
            Info =
            {
                Author = data.PersonalInformation.Name,
                Subject = $"Der Lebenslauf von {data.PersonalInformation.Name}.",
                Title = $"Lebenslauf von {data.PersonalInformation.Name}"
            }
        };

        ConfigureDocumentStyles();

        document.FootnoteLocation = FootnoteLocation.BottomOfPage;
        document.FootnoteNumberStyle = FootnoteNumberStyle.Arabic;
        document.FootnoteStartingNumber = 1;

        document.AddSection().AddParagraph($"Lebenslauf von {data.PersonalInformation.Name}", "Heading1");
        var imagePath = Path.Join(_folderProvider.WorkingDirectory, data.PersonalInformation.Image.Path);
        if (File.Exists(imagePath))
        {
            var image = document.LastSection.AddImage(imagePath);
            image.Width = Unit.FromCentimeter(data.PersonalInformation.Image.PrintWith);
        }

        AddSimpleTableAtEnd(
            new("Name", data.PersonalInformation.Name),
            new("Geburstag", data.PersonalInformation.Birthday),
            new("Gebursort", data.PersonalInformation.Birthplace),
            new("E-Mail", data.PersonalInformation.EMail),
            new("Handy", data.PersonalInformation.Mobil),
            new("Homepage", data.PersonalInformation.Homepage)
        );
        foreach (var section in data.MyPrintCv.Sections)
        {
            AddSection(section, 1);
        }

        return document;

        void AddSection(CvSection section, int level)
        {
            if (!string.IsNullOrWhiteSpace(section.Title))
            {
                var para = document.LastSection.AddParagraph(section.Title, $"Heading{level}");
                para.AddBookmark(section.Title);
            }

            if (section.Entries?.Any() ?? false)
            {
                AddEntries(section.Entries);
            }

            if (section.Skills?.Any() ?? false)
            {
                AddSkills(section.Skills);
            }

            foreach (var subSection in section.SubSections ?? Array.Empty<CvSection>())
            {
                AddSection(subSection, level + 1);
            }

            void AddEntries(CvEntry[] entries)
            {
                AddTableAtEnd(entries.Select(e =>
                    new KeyValuePair<string, (string, string, string)>(e.TimePeriod,
                        (e.ShortTitle, e.Additional, e.Text)!)).ToArray());
                document.LastSection.AddParagraph();
            }

            void AddSkills(CvSkill[] skills)
            {
                foreach (var skill in skills)
                {
                    document.LastSection.AddParagraph(skill.Text, Constants.Styles.BulletList);
                }

                document.LastSection.AddParagraph();
            }
        }

        void AddSimpleTableAtEnd(params KeyValuePair<string, string>[] data)
        {
            AddTableAtEnd(data.Select(i => new KeyValuePair<string, (string, string, string)>(i.Key, ("", "", i.Value)))
                .ToArray());
        }

        void AddTableAtEnd(params KeyValuePair<string, (string start, string addtional, string text)>[] data)
        {
            var width = document.DefaultPageSetup.PageWidth - document.DefaultPageSetup.LeftMargin -
                        document.DefaultPageSetup.RightMargin;
            var table = document.LastSection.AddTable();

            if (showBorder)
            {
                table.Borders.Color = Color.FromRgb(100, 100, 100);
                table.Borders.Style = BorderStyle.Single;
                table.Borders.Width = Unit.FromPoint(2);
                table.Borders.Visible = true;
            }

            table.AddColumn(Unit.FromPoint(GetMaxTextLength(data.Select(d => d.Key).ToArray()) + 5));
            table.AddColumn(width - table.Columns[0].Width);
            foreach (var item in data)
            {
                var row = table.AddRow();
                row.Cells[0].AddParagraph(item.Key);
                var paragraph = row.Cells[1].AddParagraph();
                if (!string.IsNullOrWhiteSpace(item.Value.start))
                {
                    paragraph.AddFormattedText($"{item.Value.start}, ", Constants.Styles.Bold);
                }

                if (!string.IsNullOrWhiteSpace(item.Value.addtional))
                    paragraph.AddFormattedText($"{item.Value.addtional}", Constants.Styles.Italic).AddLineBreak();

                if (!string.IsNullOrWhiteSpace(item.Value.text))
                    paragraph.AddText(item.Value.text);
            }

            Unit GetMaxTextLength(string[] text)
            {
                var renderer = new PdfDocumentRenderer()
                {
                    Document = new Document()
                };
                renderer.RenderDocument();
                var xGraphics = XGraphics.FromPdfPage(renderer.PdfDocument.AddPage(), XGraphicsPdfPageOptions.Prepend);
                var font = document.Styles.Normal.Font;
                var xfont = new XFont(font.Name, font.Size);

                var max = text.Select(t => xGraphics.MeasureString(t, xfont).Width).Max();

                return max;
            }
        }

        void ConfigureDocumentStyles()
        {
            //Console.WriteLine(string.Join(", ",document.Styles.AsQueryable().OfType<MigraDoc.DocumentObjectModel.Style>().Select(i => i.Name)));

            var normal = document.Styles[Constants.Styles.Normal] ??
                         throw new NullReferenceException("Normal Style not found!");
            normal.Font.Size = 12;

            var boldStyle = document.Styles.AddStyle(Constants.Styles.Bold, Constants.Styles.Normal);
            boldStyle.Font.Bold = true;
            var italicStyle = document.Styles.AddStyle(Constants.Styles.Italic, Constants.Styles.Normal);
            italicStyle.Font.Italic = true;

            var heading1 = document.Styles[Constants.Styles.Heading1];
            heading1!.Font.Bold = true;
            heading1!.Font.Size = 24;
            heading1.ParagraphFormat.KeepWithNext = true;
            heading1.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.8);
            heading1.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.2);


            var heading2 = document.Styles[Constants.Styles.Heading2];
            heading2!.Font.Bold = true;
            heading2!.Font.Size = 18;
            heading2.ParagraphFormat.KeepWithNext = true;
            heading2.ParagraphFormat.KeepTogether = true;
            heading2.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0);

            var heading3 = document.Styles[Constants.Styles.Heading3];
            heading3!.Font.Bold = true;
            heading3!.Font.Size = 16;
            heading3!.ParagraphFormat.KeepTogether = true;
            heading3.ParagraphFormat.KeepWithNext = true;

            var listStyle = document.AddStyle(Constants.Styles.BulletList, "Normal");
            listStyle.ParagraphFormat.LeftIndent = "0.5cm";
            listStyle.ParagraphFormat.ListInfo = new ListInfo
            {
                ContinuePreviousList = true,
                ListType = ListType.BulletList1
            };
            listStyle.ParagraphFormat.KeepWithNext = true;
        }
    }
}