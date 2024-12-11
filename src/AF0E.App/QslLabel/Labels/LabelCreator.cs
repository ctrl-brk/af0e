using System.Text;
using QslLabel.Labels.Pdf;
using QslLabel.Models;

namespace QslLabel.Labels;

internal static class LabelCreator
{
    internal static void CreateLabels(IEnumerable<LogGridModel> log, TemplateType templateType, int startLabelNum, FileType fileType, string fileName)
    {
        var listLog = log.ToList();
        var contacts = listLog.OrderBy(x => x.Call).ThenByDescending(x => x.UTC).GroupBy(x => x.Call).Select(g => new LabelData { Call = g.Key, Contacts = g.Select(q => q).ToList() }).ToList();

        if (contacts.Count == 0) return;

        foreach (var l in contacts)
        {
            foreach (var c in l.Contacts)
            {
                if (string.IsNullOrWhiteSpace(c.QslComment)) continue;
                l.QslComments = [.. l.QslComments, .. c.QslComment.Split('^')];
            }
        }

        if (fileType == FileType.PDF)
            CreatePdfLabels(listLog, contacts, templateType, startLabelNum, fileName);
    }

    private static void CreatePdfLabels(IEnumerable<LogGridModel> log, List<LabelData> contacts, TemplateType templateType, int startLabelNum, string fileName)
    {
        if (!PdfCreator.Generate(contacts, templateType, startLabelNum, fileName)) return;
        var sqlPath = Path.ChangeExtension(fileName, ".sql");

        var sb = new StringBuilder($"""
                                   update [HamLog].[dbo].[TABLE_HRD_CONTACTS_V01]
                                      set COL_QSL_SENT_VIA = ?'DBM', COL_QSL_SENT = 'Y', COL_QSLSDATE = '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}'
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
