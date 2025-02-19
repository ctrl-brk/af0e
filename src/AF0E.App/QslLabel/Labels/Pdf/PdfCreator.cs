using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QslLabel.Models;

namespace QslLabel.Labels.Pdf;

internal static class PdfCreator
{
    private const string DefaultFontFamily = "Consolas";
    private const string SymbolsFontFamily = "Symbols";
    private const double CharWidth = 7;

    private static readonly XFont _titleFont;
    private static readonly XFont _toFont;
    private static readonly XFont _confirmFont;
    private static readonly XFont _tableFont;
    private static readonly XFont _symbolsFont;

#pragma warning disable CA1810 // Initialize reference type static fields inline. Doesn't work, need to set the FontResolver first.
    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
    static PdfCreator()
#pragma warning restore CA1810
    {
        GlobalFontSettings.FontResolver = new FontResolver();

        _titleFont = new(DefaultFontFamily, 12, XFontStyleEx.Bold);
        _toFont = new(DefaultFontFamily, 12, XFontStyleEx.Bold);
        _confirmFont = new(DefaultFontFamily, 10, XFontStyleEx.Regular);
        _tableFont = new(DefaultFontFamily, 8, XFontStyleEx.Regular);
        _symbolsFont = new(SymbolsFontFamily, 8, XFontStyleEx.Regular);
    }

    public static bool Generate(List<LabelData> data, TemplateType templateType, int startLabelNum, bool printDeliveryMethod, string fileName)
    {
        if (!CheckFitment(data, templateType)) return false;

        var document = new PdfDocument();

        var maxLabelsPerPage = templateType switch
        {
            TemplateType.TwoByFour => 10,
            _ => 14
        };

        var printedLabels = 0;
        var pageNum = 1;

        while (printedLabels < data.Count)
        {
            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.Letter;
            var gfx = XGraphics.FromPdfPage(page);

            var labelsPerPage = pageNum == 1 ? maxLabelsPerPage - (startLabelNum - 1) : maxLabelsPerPage;
            if (labelsPerPage > data.Count - printedLabels)
                labelsPerPage = data.Count - printedLabels;

            GeneratePage(gfx, data.Slice(printedLabels, labelsPerPage), templateType, pageNum == 1 ? startLabelNum : 1, printDeliveryMethod);
            printedLabels += labelsPerPage;
            pageNum++;
        }

        try
        {
            document.Save(fileName);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
            return false;
        }
        finally
        {
            document.Close();
        }

        Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        return true;
    }

    private static void GeneratePage(XGraphics gfx, List<LabelData> data, TemplateType templateType, int startLabelNum, bool printDeliveryMethod)
    {
        const double LabelWidth = 271; // 4 inches + 1pt
        const double HorizontalGap = 32;

        double labelHeight, marginTop, marginLeft;

        switch (templateType)
        {
            case TemplateType.TwoByFour:
                marginTop = 43;
                marginLeft = 19.2756;
                labelHeight = 144;
                break;
            default: // TemplateType.OneAndThirdByFour:
                marginTop = 64.8;
                marginLeft = 20;
                labelHeight = 95.5;
                break;
        }

        var startRow = startLabelNum / 2 + startLabelNum % 2;
        var startCol = startLabelNum % 2 > 0 ? 1 : 2;

        var row = startRow;
        var col = startCol;

        foreach (var label in data)
        {
            var pota = label.Contacts.Any(x => !string.IsNullOrWhiteSpace(x.Parks));
            var maxCountyLength = pota ? label.Contacts.Max(x => x.MyCounty?.Length ?? 0) + 3 : 0; //3 is for ,CO

            var startX = marginLeft + (col - 1) * (LabelWidth + HorizontalGap);
            var startY = marginTop + (row - 1) * labelHeight;
#if DEBUG
            gfx.DrawRectangle(new XPen(XColors.LightGray, 0.5), startX, startY, LabelWidth, labelHeight);
#endif
            var curX = startX; var curY = startY - 2;
            Draw("To Radio:", _titleFont);
            curX += 65;
            Draw(label.Call.Replace('0', 'Ø'), _toFont);

            var via = label.Contacts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.QslMgrCall));
            if (via != null)
            {
                curX += (label.Call.Length + 1) * CharWidth; // call plus space
                if (!via.QslMgrCall!.StartsWith("via ", StringComparison.OrdinalIgnoreCase))
                {
                    Draw("via ", _titleFont);
                    curX += 4 * CharWidth;
                }
                Draw(via.QslMgrCall!.Replace('0', 'Ø'), _titleFont);
            }

            if (printDeliveryMethod && !string.IsNullOrEmpty(label.Delivery) && (label.Delivery == "B" || label.Delivery == "D")) //buro or direct
            {
                curX = startX + 262; curY = startY + 2;
                Draw(label.Delivery == "B" ? "A" : "i", _symbolsFont);
            }

            curX = startX; curY = startY + 13;
            Draw($"AFØE confirms the following QSO{(label.Contacts.Count > 1 ? "s" : "")}:", _confirmFont);
            curX += 3; curY += 17;

            var header = pota
                ? "YY-MM-DD  UTC  Mhz Mode RST POTA     County            "
                : "Date (YMD)    UTC      Band      Mode      RST";

            var shortGrid = false;
            var noGrid = false;
            if (pota)
            {
                if (maxCountyLength > 17)
                {
                    header += "  ";
                    shortGrid = true;
                }
                if (maxCountyLength < 20)
                    header += "Grid";
                else
                    noGrid = true;
            }

            Draw(header);

            curX = startX; curY += 11;
            Draw("", null, pota ? 272 : 210);

            foreach (var q in label.Contacts)
            {
                curX = startX + 3;

                string qsoStr;

                if (pota)
                {
                    var grid = noGrid || string.IsNullOrWhiteSpace(q.Parks) ? "" : shortGrid ? q.MyGrid?[..4] : q.MyGrid;
                    var county = q.MyCounty;
                    if (county is { Length: <= 21 })
                        county += $",{q.MyState}";

                    county = maxCountyLength switch
                    {
                        > 17 when county != null => county.PadRight(19), // 4 char grid
                        < 18 when county != null => county.PadRight(17), // leave space for 6 char grid
                        _ => county
                    };

                    qsoStr = $"{q.UTC:yy-MM-dd} {q.UTC:hh:mm} {q.Mhz,-3} {q.Mode,-3}  {q.RST,-3} {q.Parks,-8} {county} {grid}";
                }
                else
                    qsoStr = $"{q.UTC:yyyy-MM-dd}   {q.UTC:hh:mm}     {q.Band,-4}      {q.Mode,3}       {q.RST}";

                Draw(qsoStr);
                curY += 8;
            }

            if (label.QslComments.Length > 0)
            {
                curY += 2;
                Draw("", null, pota ? 272 : 210);
                curY++;

                foreach (var c in label.QslComments)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    Draw(c);
                    curY += 8;
                }
            }

            if (col == 2)
            {
                col = 1;
                row++;
            }
            else
                col++;

            continue;

            void Draw(string msg, XFont? font = null, double? lineLength = null, XBrush? brush = null)
            {
                if (lineLength != null)
                {
                    gfx.DrawLine(new XPen(XColors.Black, 0.5), new XPoint(curX, curY), new XPoint(curX + lineLength.Value, curY));
#if DEBUG
                    Debug.WriteLine("------------------------------------------------------");
#endif
                }
                else
                {
                    brush ??= XBrushes.Black;
                    gfx.DrawString(msg, font ?? _tableFont, brush, curX, curY, XStringFormats.TopLeft);
#if DEBUG
                    Debug.WriteLine(msg);
#endif
                }
            }
        }
    }

    private static bool CheckFitment(List<LabelData> data, TemplateType templateType)
    {
        var maxQso = templateType switch
        {
            TemplateType.TwoByFour => 13,
            _ => 6
        };

        var msg = data.Where(label => label.Contacts.Count + label.QslComments.Length> maxQso).Aggregate("", (current, label) => current + $"{label.Call} has {label.Contacts.Count} QSOs {(label.QslComments.Length > 0 ? $" and {label.QslComments.Length} comments." : "")}.\n");

        return string.IsNullOrEmpty(msg) || MessageBox.Show(
            $"{msg}Are you sure you want to continue?",
            $"Max Lines = {maxQso}",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) != DialogResult.Cancel;
    }
}
