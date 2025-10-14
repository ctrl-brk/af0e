using System.Data;
using Microsoft.EntityFrameworkCore;
using QslLabel.Models;

namespace QslLabel;

partial class MainForm
{
    private async Task DoSearch(bool analyze)
    {
        if (btnSave.Enabled && MessageBox.Show("Reload anyway?", "There are unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        int printCountB = 0, printCountD = 0;

        if (!analyze && string.IsNullOrEmpty(tbCall.Text))
            cbQueued.Checked = true;

        _highlightedRowIndex = 0;

        Cursor = Cursors.WaitCursor;

        _contactsFromDb = await _dbContext.Database.SqlQuery<LogGridModel>(
            $"""
             exec GetQslLabelData
                  @Call={(string.IsNullOrEmpty(tbCall.Text) ? null : tbCall.Text)},
                  @QueuedOnly={cbQueued.Checked},
                  @StartDate={(dtpStartDate.Checked ? dtpStartDate.Value : DateTime.MinValue)},
                  @EndDate={(dtpEndDate.Checked ? dtpEndDate.Value : DateTime.MaxValue)},
                  @Analyze={analyze},
                  @DxOnly={cbDxOnly.Checked},
                  @ShowWaiting = {cbShowWaiting.Checked}
             """
        ).ToListAsync();

        if (_contactsFromDb.Count == 0)
        {
            Cursor = Cursors.Default;
            MessageBox.Show("Not in log!", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return;
        }

        if (analyze)
            _contactsFromDb = [.. _contactsFromDb.OrderBy(x => x.Country).ThenBy(x => x.Call).ThenByDescending(x => x.sQSL).ThenByDescending(x => x.UTC)];

        foreach (var q in _contactsFromDb)
        {
            q.Mhz = q.Band switch
            {
                "160m" => "1.8",
                "80m" => "3.5",
                "40m" => "7",
                "30m" => "10",
                "20m" => "14",
                "17m" => "18",
                "15m" => "21",
                "12m" => "24",
                "10m" => "28",
                "6m" => "50",
                "2m" => "144",
                "70cm" => "420",
                _ => ""
            };

            if (q.Metadata!.Equals("P", StringComparison.OrdinalIgnoreCase) || q.Metadata!.Equals("Print", StringComparison.OrdinalIgnoreCase))
            {
                if (q.QslDeliveryMethod == "B") printCountB++;
                else if (q.QslDeliveryMethod == "D") printCountD++;
            }
        }

        PopulateGrid();
        UpdateStatus(printCountB, printCountD);

        Cursor = Cursors.Default;
        //styles applied in gridLog_DataBindingComplete()
    }

    private void UpdateStatus(int printCountB, int printCountD)
    {
        lblStatus.Text = $"{_contactsList.Count:#,##0} ({_contactsList.DistinctBy(x => x.Call).Count():#,##0} calls) qso's";

        if (printCountB + printCountD > 0)
            lblStatus.Text += $". {printCountB + printCountD:#,##0} labels: {printCountB:#,##0} buro, {printCountD:#,##0} direct";
    }
}
