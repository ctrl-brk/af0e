using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using QslLabel.Forms.LogForm;
using QslLabel.Models;

namespace QslLabel;

partial class MainForm
{
    private ContextMenuStrip _contextMenu;
    private int _selectedRowIndex;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
    private void SetupContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        _contextMenu.Items.AddRange([
            new ToolStripMenuItem("Country log...", null, MenuCountryLog_Click),
            new ToolStripMenuItem("Call log...", null, MenuCallLog_Click),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Copy QSO details", null, MenuQsoCopy_Click),
            new ToolStripMenuItem("Copy ADIF", null, MenuAdifCopy_Click),
            new ToolStripMenuItem("Copy Call", null, MenuCallCopy_Click),
        ]);
    }

    private void HandleContextMenu(DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0)
            return;

        //gridLog.ClearSelection();
        //gridLog.Rows[e.RowIndex].Selected = true;
        _selectedRowIndex = e.RowIndex;
        _contextMenu.Show(gridLog, gridLog.PointToClient(Cursor.Position));
    }

    private void MenuCallLog_Click(object? sender, EventArgs e)
    {
        var call = ((BindingList<LogGridModel>)gridLog.DataSource!)[_selectedRowIndex].Call;
        using var form = new LogForm("Call", call, _dbContext) { Text = $"{call} log" };
        form.ShowDialog();
    }

    private void MenuCountryLog_Click(object? sender, EventArgs e)
    {
        var country = ((BindingList<LogGridModel>)gridLog.DataSource!)[_selectedRowIndex].Country;
        using var form = new LogForm("Country", country, _dbContext) { Text = $"{country.ToUpper()} log" };
        form.ShowDialog();
    }

    private void MenuCallCopy_Click(object? sender, EventArgs e) => Clipboard.SetText(((BindingList<LogGridModel>)gridLog.DataSource!)[_selectedRowIndex].Call);

    private void MenuQsoCopy_Click(object? sender, EventArgs e) => CopyData("Q");

    private void MenuAdifCopy_Click(object? sender, EventArgs e) => CopyData("A");

    private void CopyData(string type)
    {
        var selectedRows = new List<LogGridModel>();

        if (gridLog.SelectedRows.Count > 0)
        {
            selectedRows.AddRange(from DataGridViewRow row in gridLog.SelectedRows select ((BindingList<LogGridModel>)gridLog.DataSource!)[row.Index]);
        }
        else
        {
            selectedRows.Add(((BindingList<LogGridModel>)gridLog.DataSource!)[_selectedRowIndex]);
        }

        var text = type switch
        {
            "Q" => GenQsoData(selectedRows),
            "A" => GenAdifData(selectedRows),
            _ => ""
        };

        Clipboard.SetText(text);
    }

    private static string GenQsoData(List<LogGridModel> rows)
    {
        var sb = new StringBuilder("Hi.\n\nI would like to get a QSL card for the following QSO");
        if (rows.Count > 1) sb.Append('s');
        sb.AppendLine(":");

        return rows.Aggregate(sb, (s, q) => s.AppendLine($"{q.UTC:yyyy-MM-dd HH:mm} UTC {q.Band} {q.Mode} {q.RST} {q.Call}")).ToString();
    }

    private static string GenAdifData(List<LogGridModel> rows) => rows.Aggregate(new StringBuilder(), (sb, qso) => sb.AppendLine(qso.ToAdif())).ToString();
}
