using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

public interface ITextChunkerService
{
  IEnumerable<string> GetParagraphs(string text);
  IEnumerable<string> GetParagraphs(IEnumerable<string> text);
  IEnumerable<string> GetLines(string text);
  IEnumerable<string> SplitPdf(Stream stream);
  IEnumerable<string> SplitWord(Stream stream);
  IEnumerable<string> SplitExcel(Stream stream);
}

public partial class TextChunkerService : ITextChunkerService
{
  public TextChunkerService()
  {
  }

  public IEnumerable<string> GetParagraphs(string text)
  {
    return text.ReplaceLineEndings().Split(new string[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
  }

  public IEnumerable<string> GetParagraphs(IEnumerable<string> pages)
  {
    var collection = new List<string>();
    foreach (var page in pages)
    {
      var range = page.ReplaceLineEndings().Split(new string[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      collection.AddRange(range);
    }
    return collection;
  }

  public IEnumerable<string> GetLines(string text)
  {
    return text.ReplaceLineEndings().Split(Environment.NewLine, StringSplitOptions.None);
  }

  public IEnumerable<string> SplitPdf(Stream stream)
  {
    var strategy = new SimpleTextExtractionStrategy();
    using PdfReader reader = new(stream);
    using PdfDocument pdf = new(reader);
    var pages = pdf.GetNumberOfPages();
    for (int i = 1; i <= pages; i++)
    {
      var page = PdfTextExtractor.GetTextFromPage(pdf.GetPage(i), strategy);
      yield return page;
    }
  }

  public IEnumerable<string> SplitWord(Stream stream)
  {
    using var doc = WordprocessingDocument.Open(stream, false);
    var text = doc?.MainDocumentPart?.Document?.Body?.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>() ?? new List<DocumentFormat.OpenXml.Wordprocessing.Text>();
    var chucks = new List<string>();
    foreach (var item in text) 
    {
      yield return item.Text;
    }
  }

  public IEnumerable<string> SplitExcel(Stream stream)
  {
    using var doc = SpreadsheetDocument.Open(stream, false);
    var part = doc?.WorkbookPart?.WorksheetParts.FirstOrDefault();
    if(part is null) yield break;
    var sheets = part.Worksheet.Elements<SheetData>().ToList();
    foreach (var sheet in sheets)
    {
      foreach (var row in sheet.Elements<Row>())
      {
        var builder = new StringBuilder();
        foreach (var cell in row.Elements<Cell>())
        {
          var text = cell?.CellValue?.InnerText ?? string.Empty;
          builder.Append('"' + text.Replace("\"", "\"\"") + "\",");
        }
        yield return builder.ToString();
      } 
    }
  }

}