using QslLabel.Labels;
using QslLabel.Models;

namespace QslLabel;

partial class MainForm
{
    private void cmbTemplate_SelectedValueChanged(object sender, EventArgs e)
    {
        var selectedItem = (string)cmbTemplate.SelectedItem!;

        cmbStartLabelNum.Items.Clear();

        var maxLabels = selectedItem.StartsWith("1.3") ? 14 : 10;

        for (var i = 1; i <= maxLabels; i++)
            cmbStartLabelNum.Items.Add(i.ToString());

        cmbStartLabelNum.SelectedIndex = 0;

        btnGenPdf.Enabled = gridLog.SelectedRows.Count > 0;
    }

    private async void btnSearch_Click(object sender, EventArgs e)
    {
        tbCall.SelectAll();
        HighlightMarkSentBtn(false);
        btnMarkSent.Enabled = false;
        cbShowWaiting.Checked = true;
        await DoSearch(false);
    }

    private async void btnAnalyze_Click(object sender, EventArgs e)
    {
        await DoSearch(true);
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in gridLog.Rows)
        {
            if (row.DataBoundItem == null) continue;

            ((LogGridModel)row.DataBoundItem!).SaveChanges(_dbContext);
            row.Cells["revCol"].Value = "";
            btnSave.Enabled = false;
            _dirtyRowsCount = 0;
        }
    }

    private void btnSelectPrint_Click(object sender, EventArgs e)
    {
        var callIdx = gridLog.Columns[CallColName]!.Index;
        var metaIdx = gridLog.Columns["Metadata"]!.Index;
        var selectionChanged = false;

        for (var idx = 0; idx < gridLog.Rows.Count;)
        {
            var row = gridLog.Rows[idx];

            //if (row.IsNewRow) continue;  // skip new rows, but I don't have them :)

            //select all rows with "P" or "Print" in Metadata column along with all their siblings based on call sign
            var meta = row.Cells[metaIdx].Value?.ToString() ?? "";

            if (meta.Equals("P", StringComparison.OrdinalIgnoreCase) || meta.Equals("Print", StringComparison.OrdinalIgnoreCase))
            {
                //set or toggle selection
                row.Selected = (Control.ModifierKeys & Keys.Control) != Keys.Control || !row.Selected;

                selectionChanged = true;

                var call = row.Cells[callIdx].Value!.ToString();
                idx++;
                while (idx < gridLog.Rows.Count && gridLog.Rows[idx].Cells[callIdx].Value!.ToString() == call)
                {
                    gridLog.Rows[idx].Selected = (Control.ModifierKeys & Keys.Control) != Keys.Control || !gridLog.Rows[idx].Selected;
                    idx++;
                }
            }
            else
                idx++;
        }

        if (selectionChanged)
            gridLog_SelectionChanged(gridLog, null);
    }

    private void btnGenPdf_Click(object sender, EventArgs e)
    {
        var res = MessageBox.Show("Print emojis?", "Delivery method", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (res == DialogResult.Cancel) return;

        saveDlg.FileName = cmbTemplate.SelectedItem switch
        {
            "1.3 x 4" => "!QSL Labels 1.3x4", //exclamation sign is to keep it on top of the explorer's file list sorted by name
            "2.0 x 4" => "!QSL Labels 2x4",
        };

        if (!string.IsNullOrWhiteSpace(tbCall.Text))
            saveDlg.FileName += $" - {tbCall.Text}";

        if (saveDlg.ShowDialog() == DialogResult.Cancel)
            return;

        GeneratePdf(res == DialogResult.Yes, saveDlg.FileName);
    }

    private void GeneratePdf(bool printDeliveryMethod, string fileName)
    {
        TemplateType templateType = cmbTemplate.SelectedItem switch
        {
            "1.3 x 4" => TemplateType.OneAndThirdByFour,
            "2.0 x 4" => TemplateType.TwoByFour,
        };

        if (LabelCreator.CreateLabels(from DataGridViewRow row in gridLog.SelectedRows select (LogGridModel)row.DataBoundItem!, templateType, int.Parse(cmbStartLabelNum.SelectedItem!.ToString()!), printDeliveryMethod, FileType.PDF, fileName))
            HighlightMarkSentBtn(true);
    }

    private void btnMarkSent_Click(object sender, EventArgs e)
    {
        HighlightMarkSentBtn(false);

        foreach (DataGridViewRow row in gridLog.SelectedRows)
        {
            ((LogGridModel)row.DataBoundItem!).sQSL = "Y";
        }
    }

    private void cbViewMyLocation_Click(object? sender, EventArgs? e)
    {
        gridLog.Columns["MyGrid"]!.Visible = gridLog.Columns["MyState"]!.Visible = gridLog.Columns["MyCity"]!.Visible = gridLog.Columns["MyCounty"]!.Visible = cbViewMyLocation.Checked;
    }

    private void cbShowWaiting_Click(object sender, EventArgs e)
    {
        PopulateGrid();
    }
}
