using System.Text;
using QslLabel.Labels.Pdf;
using QslLabel.Models;

namespace QslLabel.Labels;

internal static class LabelCreator
{
    internal static bool CreateLabels(IEnumerable<LogGridModel> log, TemplateType templateType, int startLabelNum, bool printDeliveryMethod, FileType fileType, string fileName)
    {
        var listLog = log.ToList();

        var labels = listLog
            .OrderBy(x => string.IsNullOrEmpty(x.QslMgrCall) ? x.Call : x.QslMgrCall)
            .ThenByDescending(x => x.UTC)
            .GroupBy(x => x.Call)
            .Select(g => new LabelData
            {
                Call = g.Key,
                Contacts = [.. g.Select(q => q)],
                TotalContacts = g.Count(),
            })
            .ToList();

        UpdateProperties(labels);

        if (labels.Count == 0)
            return false;

        var maxRows = templateType == TemplateType.TwoByFour ? 11 : 6;

        // Split labels with more than maxRows contacts into multiple labels with no header except for the first
        var splitLabels = new List<LabelData>();
        foreach (var label in labels)
        {
            if (label.Contacts.Count <= maxRows)
            {
                splitLabels.Add(label);
                continue;
            }

            var maxContactsPerLabel = maxRows;
            for (var i = 0; i < label.Contacts.Count; i += maxContactsPerLabel)
            {
                if (i > 0)
                    maxContactsPerLabel = maxRows + 5;

                var contactsChunk = label.Contacts.Skip(i).Take(maxContactsPerLabel).ToList();

                splitLabels.Add(new LabelData
                {
                    Call = label.Call,
                    Delivery = label.Delivery,
                    Contacts = contactsChunk,
                    PrintHeader = i == 0,
                    HasPota = label.HasPota,
                    MaxCountyLength = label.MaxCountyLength,
                    TotalContacts = label.TotalContacts,
                });
            }
        }

        foreach (var l in splitLabels)
        {
            foreach (LogGridModel c in l.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.QslComment)))
            {
                l.QslComments = [.. l.QslComments, .. c.QslComment!.Split('^')];
            }
        }

        if (fileType == FileType.PDF)
            CreatePdfLabels(listLog, splitLabels, templateType, startLabelNum, printDeliveryMethod, fileName);

        return true;
    }

    private static void UpdateProperties(List<LabelData> labels)
    {
        foreach (var label in labels)
        {
            foreach (var c in label.Contacts.Where(x => string.IsNullOrEmpty(x.MyCounty)))
            {
                c.MyCounty = AppSettings.DefaultCounty;
                c.MyState = AppSettings.DefaultState;
                c.MyGrid = AppSettings.DefaultGrid;
            }

            label.Delivery = label.Contacts.FirstOrDefault(x => !string.IsNullOrEmpty(x.QslDeliveryMethod))?.QslDeliveryMethod ?? string.Empty;
            label.HasPota = label.Contacts.Any(x => !string.IsNullOrWhiteSpace(x.Parks));
            label.MaxCountyLength = label.HasPota ? label.Contacts.Max(x => x.MyCounty?.Length ?? 0) + 3 : 0; //3 is for ,CO
        }
    }

    private static bool CreatePdfLabels(IEnumerable<LogGridModel> log, List<LabelData> contacts, TemplateType templateType, int startLabelNum, bool printDeliveryMethod, string fileName)
    {
        if (!PdfCreator.Generate(contacts, templateType, startLabelNum, printDeliveryMethod, fileName))
            return false;

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

        return true;
    }
}
