using System.Text;
using QslLabel.Labels.Pdf;
using QslLabel.Models;

namespace QslLabel.Labels;

internal static class LabelCreator
{
    internal static void CreateLabels(IEnumerable<LogGridModel> log, TemplateType templateType, int startLabelNum, bool printDeliveryMethod, FileType fileType, string fileName)
    {
        var listLog = log.ToList();
        var contacts = listLog.OrderBy(x => string.IsNullOrEmpty(x.QslMgrCall) ? x.Call : x.QslMgrCall).ThenByDescending(x => x.UTC).GroupBy(x => x.Call).Select(g => new LabelData { Call = g.Key, Delivery = g.First().QslDeliveryMethod, Contacts = g.Select(q => q).ToList() }).ToList();

        if (contacts.Count == 0) return;

        foreach (var l in contacts)
        {
            foreach (LogGridModel c in l.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.QslComment)))
            {
                l.QslComments = [.. l.QslComments, .. c.QslComment!.Split('^')];
            }
        }

        if (fileType == FileType.PDF)
            CreatePdfLabels(listLog, contacts, templateType, startLabelNum, printDeliveryMethod, fileName);
    }

    private static void CreatePdfLabels(IEnumerable<LogGridModel> log, List<LabelData> contacts, TemplateType templateType, int startLabelNum, bool printDeliveryMethod, string fileName)
    {
        if (!PdfCreator.Generate(contacts, templateType, startLabelNum, printDeliveryMethod, fileName)) return;
        var sqlPath = Path.ChangeExtension(fileName, ".sql");

        var sb = new StringBuilder($"""
                                   update [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01]
                                      set COL_QSL_SENT = 'Y', COL_QSLSDATE = '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}'
                                    where COL_PRIMARY_KEY in (

                                   """);
        foreach (var l in log)
        {
            sb.AppendLine($"{l.ID}, -- '{l.Call}'");
        }
        sb.AppendLine(")");

        File.WriteAllText(sqlPath, sb.ToString());
    }
}
