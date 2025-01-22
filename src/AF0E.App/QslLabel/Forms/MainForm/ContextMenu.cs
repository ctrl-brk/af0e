using System.Diagnostics.CodeAnalysis;
using QslLabel.Forms.LogForm;
using QslLabel.Models;

namespace QslLabel;

internal partial class MainForm : Form
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
        var call = ((List<LogGridModel>)gridLog.DataSource!)[_selectedRowIndex].Call;
        var form = new LogForm("Call", call, _dbContext) { Text = $"{call} log"};
        form.ShowDialog();
    }

    private void MenuCountryLog_Click(object? sender, EventArgs e)
    {
        var country = ((List<LogGridModel>)gridLog.DataSource!)[_selectedRowIndex].Country;
        var form = new LogForm("Country", country, _dbContext) { Text = $"{country.ToUpper()} log"};
        form.ShowDialog();
    }
}
