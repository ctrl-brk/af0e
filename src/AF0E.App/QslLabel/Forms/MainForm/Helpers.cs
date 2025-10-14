using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using QslLabel.Models;

namespace QslLabel;

partial class MainForm
{
    private void PopulateGrid()
    {
        if (_contactsList != null)
            _contactsList.ListChanged -= ContactChanged;

        _contactsList = [.. _contactsFromDb.Where(x => cbShowWaiting.Checked || x.Metadata != "W")];
        _contactsList.ListChanged += ContactChanged;
        gridLog.DataSource = _contactsList;

        cbViewMyLocation.Enabled = cbShowWaiting.Enabled = _contactsList.Count > 0;

        cbViewMyLocation_Click(null, null);
    }

    private void ContactChanged(object? sender, ListChangedEventArgs e)
    {
        gridLog.Rows[e.NewIndex].Cells["revCol"].Style.Font = new Font("Wingdings 3", 11);
        gridLog.Rows[e.NewIndex].Cells["revCol"].Value = "Q"; //revert symbol
        _dirtyRowsCount++;
        btnSave.Enabled = true;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private void ConvertCallColumnToLink()
    {
        if (gridLog.Columns[CallColName] != null)
            return;

        var column = gridLog.Columns["Call"]!;

        DataGridViewLinkColumn linkColumn = new()
        {
            Name = CallColName,
            HeaderText = column.HeaderText,
            DataPropertyName = column.DataPropertyName,
            LinkColor = Color.DarkBlue,
            ActiveLinkColor = Color.Blue,
            VisitedLinkColor = Color.DarkBlue,
            LinkBehavior = LinkBehavior.HoverUnderline,
            UseColumnTextForLinkValue = false
        };

        var idx = column.Index;
        gridLog.Columns.RemoveAt(idx);
        gridLog.Columns.Insert(idx, linkColumn);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private bool ConvertQslSentColumnToComboBox()
    {
        if (gridLog.Columns[QslSentColName] != null)
            return false;

        var cbCol = new DataGridViewComboBoxColumn
        {
            Width = 50,
            HeaderText = "sQSL",
            Name = QslSentColName,
            DataPropertyName = "sQSL",
            DataSource = _sQslDataSource
        };

        var idx = gridLog.Columns["sQSL"]!.Index;
        gridLog.Columns.RemoveAt(idx);
        gridLog.Columns.Insert(idx, cbCol);
        return true;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private bool ConvertViaColumnToComboBox()
    {
        if (gridLog.Columns[QslDeliveryColName] != null)
            return false;

        var cbCol = new DataGridViewComboBoxColumn
        {
            Width = 80,
            HeaderText = "Delivery",
            Name = QslDeliveryColName,
            DataPropertyName = "QslDeliveryMethod",
            ValueMember = "Value",
            DisplayMember = "Name",
            DataSource = _viaDataSource,
        };

        var idx = gridLog.Columns["QslDeliveryMethod"]!.Index;
        gridLog.Columns.RemoveAt(idx);
        gridLog.Columns.Insert(idx, cbCol);
        return true;
    }

    private void AddCustomColumns()
    {
        if (gridLog.Columns["chbMarkCol"] != null)
            return;

        var chbCol = new DataGridViewCheckBoxColumn
        {
            HeaderText = "Mark",
            Width = 50,
            Name = "chbMarkCol",
        };
        gridLog.Columns.Add(chbCol);


        var revCol = new DataGridViewTextBoxColumn
        {
            HeaderText = "",
            Name = "revCol",
            Width = 25,
        };
        gridLog.Columns.Add(revCol);
    }

    private void ApplyGridStyle()
    {
        //gridLog.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        //gridLog.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        gridLog.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical;
        gridLog.Columns["UTC"]!.DefaultCellStyle.Format = "yy-MM-dd HH:mm:ss";

        gridLog.Columns["ID"]!.ReadOnly = gridLog.Columns["Country"]!.ReadOnly = true;
        gridLog.Columns["UTC"]!.Width = 105;
        gridLog.Columns["rQSL"]!.Width = gridLog.Columns["lQSL"]!.Width = gridLog.Columns["Mode"]!.Width = gridLog.Columns["Band"]!.Width = gridLog.Columns["Mhz"]!.Width = gridLog.Columns["RST"]!.Width = 40;
        gridLog.Columns["Sat"]!.Width = gridLog.Columns["MyState"]!.Width = gridLog.Columns["ID"]!.Width = 50;
        gridLog.Columns["CountryQslStatus"]!.Visible = gridLog.Columns["IsDirty"]!.Visible = false;
        ((DataGridViewTextBoxColumn)gridLog.Columns["QrzQslInfo"]!).MaxInputLength = 64;
        ((DataGridViewTextBoxColumn)gridLog.Columns["SiteComment"]!).MaxInputLength = 64;
        ((DataGridViewTextBoxColumn)gridLog.Columns["QslComment"]!).MaxInputLength = 64;
        ((DataGridViewTextBoxColumn)gridLog.Columns["QslMgrCall"]!).MaxInputLength = 64;
        ((DataGridViewTextBoxColumn)gridLog.Columns["Comment"]!).DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        ((DataGridViewTextBoxColumn)gridLog.Columns["Comment"]!).MaxInputLength = 4000;
        ((DataGridViewTextBoxColumn)gridLog.Columns["Comment"]!).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        ((DataGridViewTextBoxColumn)gridLog.Columns["Metadata"]!).MaxInputLength = 64;
        gridLog.Columns[QslDeliveryColName]!.HeaderCell.ToolTipText = "Ctrl - single cell";
    }

    private void ApplyRowStyles()
    {
        var defaultStyle = true;
        var qso = (LogGridModel)gridLog.Rows[0].DataBoundItem!;
        var call = qso.Call;

        foreach (DataGridViewRow row in gridLog.Rows)
        {
            qso = (LogGridModel)row.DataBoundItem!;
            if (qso.Call != call)
            {
                call = qso.Call;
                defaultStyle = !defaultStyle;
            }

            if (!defaultStyle)
                row.DefaultCellStyle.BackColor = Color.FromArgb(235, 235, 235);
        }
    }

    private void ColorizeCallSiblings(int rowIdx, int colIdx)
    {
        if (rowIdx >= gridLog.RowCount - 1) return;

        var row = gridLog.Rows[rowIdx];
        var callIdx = gridLog.Columns[CallColName]!.Index;
        var call = row.Cells[callIdx].Value!.ToString()!;
        var backColor = row.Cells[colIdx].Style.BackColor;
        var foreColor = row.Cells[colIdx].Style.ForeColor;

        while (++rowIdx < gridLog.Rows.Count && call == gridLog.Rows[rowIdx].Cells[callIdx].Value!.ToString()!)
        {
            gridLog.Rows[rowIdx].Cells[colIdx].Style.BackColor = backColor;
            gridLog.Rows[rowIdx].Cells[colIdx].Style.ForeColor = foreColor;
        }
    }

    private void ApplyValueToSiblings(int rowIdx, int colIdx, string value, int step)
    {
        var callIdx = gridLog.Columns[CallColName]!.Index;
        var call = gridLog.Rows[rowIdx].Cells[callIdx].Value!.ToString();

        rowIdx += step;

        while (rowIdx < gridLog.RowCount && rowIdx >= 0)
        {
            if (gridLog.Rows[rowIdx].Cells[callIdx].Value!.ToString() != call) break;
            gridLog.Rows[rowIdx].Cells[colIdx].Value = value;
            rowIdx += step;
        }
    }

    private System.Windows.Forms.Timer flashTimer = new();
    private bool flashState;
    private void HighlightMarkSentBtn(bool highLight)
    {
        if (highLight)
        {
            if (flashTimer.Enabled)
                return;

            flashTimer.Interval = 500;
            flashState = true;
            flashTimer.Tick += FlashTimer_Tick;
            flashTimer.Start();
        }
        else if (flashTimer.Enabled)
        {
            flashTimer.Stop();
            flashTimer.Tick -= FlashTimer_Tick;
            btnMarkSent.BackColor = SystemColors.Control;
        }
    }

    private void FlashTimer_Tick(object? sender, EventArgs e)
    {
        btnMarkSent.BackColor = flashState ? Color.Gold : SystemColors.Control;
        flashState = !flashState;
    }
}
