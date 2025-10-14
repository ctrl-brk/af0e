using System.Diagnostics;
using QslLabel.Models;

namespace QslLabel;

partial class MainForm
{
    private void gridLog_SelectionChanged(object sender, EventArgs? e)
    {
        btnGenPdf.Enabled = gridLog.SelectedRows.Count > 0 && cmbTemplate.SelectedItem != null;
        btnMarkSent.Enabled = gridLog.SelectedRows.Count > 0;
    }

    private void gridLog_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
        if (!gridLog.IsCurrentCellDirty) return;

        if (gridLog.CurrentCell is DataGridViewComboBoxCell)
            gridLog.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void gridLog_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
    {
        string txt;

        switch (e.Column.Name)
        {
            case "lQSL": txt = "LoTW QSL Status"; break;
            case QslSentColName: txt = "QSL Status (Ctrl - single cell)"; break;
            case "rQSL": txt = "QSL Received"; break;
            case "QslComment": txt = "Printed on Label"; break;
            case "ID": txt = "HRD ID"; break;
            default: return;
        }

        e.Column.HeaderCell.ToolTipText = txt;
    }

    private void gridLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        string curCol;

        switch (gridLog.Columns[e.ColumnIndex].Name)
        {
            case "Band": curCol = "Band"; break;
            case "Mode": curCol = "Mode"; break;
            case "Country": curCol = "Country"; break;
            case "lQSL": curCol = "LoTW"; break;
            case "rQSL": curCol = "RcvQsl"; break;
            case "Metadata": curCol = "Meta"; break;
            default: return;
        }

        var statusIdx = gridLog.Columns["CountryQslStatus"]!.Index;
        var status = Enum.Parse<QslStatus>(gridLog.Rows[e.RowIndex].Cells[statusIdx].Value!.ToString()!);
        var lotwStatus = gridLog.Rows[e.RowIndex].Cells[gridLog.Columns["lQSL"]!.Index].Value!.ToString()!;
        var rcvQsl = gridLog.Rows[e.RowIndex].Cells[gridLog.Columns["rQSL"]!.Index].Value!.ToString()!;
        var meta = gridLog.Rows[e.RowIndex].Cells[gridLog.Columns["Metadata"]!.Index].Value as string ?? ""; //value is null when cell is empty
        var cell = gridLog.Rows[e.RowIndex].Cells[e.ColumnIndex]!;

        switch (curCol)
        {
            case "Band":
                switch (status)
                {
                    case QslStatus.ReceivedBandMode:
                    case QslStatus.ReceivedBand:
                        cell.Style.BackColor = Color.DarkGreen;
                        cell.Style.ForeColor = Color.LimeGreen;
                        break;
                    case QslStatus.SentBandMode:
                    case QslStatus.SentBand:
                        cell.Style.BackColor = Color.Yellow;
                        cell.Style.ForeColor = Color.Black;
                        break;
                    case QslStatus.None:
                        cell.Style.BackColor = Color.DarkRed;
                        cell.Style.ForeColor = Color.Yellow;
                        break;
                }

                break;
            case "Mode":
                switch (status)
                {
                    case QslStatus.ReceivedBandMode:
                        cell.Style.BackColor = Color.DarkGreen;
                        cell.Style.ForeColor = Color.LimeGreen;
                        break;
                    case QslStatus.SentBandMode:
                        cell.Style.BackColor = Color.Yellow;
                        cell.Style.ForeColor = Color.Black;
                        break;
                    case QslStatus.None:
                        cell.Style.BackColor = Color.DarkRed;
                        cell.Style.ForeColor = Color.Yellow;
                        break;
                }

                break;
            case "Country":
                {
                    var callIdx = gridLog.Columns[CallColName]!.Index;
                    var call = gridLog.Rows[e.RowIndex].Cells[callIdx].Value!.ToString()!;
                    var specialCall = call.Length == 3; //only 1x1 for now

                    if (specialCall)
                    {
                        cell.Style.BackColor = Color.DarkRed;
                        cell.Style.ForeColor = Color.Yellow;
                        cell.ToolTipText = "1X1";
                    }
                    else switch (status)
                        {
                            case QslStatus.ReceivedBandMode:
                                cell.Style.BackColor = Color.DarkGreen;
                                cell.Style.ForeColor = Color.LimeGreen;
                                cell.ToolTipText = "Rcvd band & mode";
                                break;
                            case QslStatus.ReceivedBand:
                                cell.Style.BackColor = Color.DarkGreen;
                                cell.Style.ForeColor = Color.Yellow;
                                cell.ToolTipText = "Rcvd band only";
                                break;
                            case QslStatus.ReceivedCountry:
                                cell.Style.BackColor = Color.DarkGreen;
                                cell.Style.ForeColor = Color.Red;
                                cell.ToolTipText = "Rcvd other band";
                                break;
                            case QslStatus.SentBandMode:
                                cell.Style.BackColor = Color.Yellow;
                                cell.Style.ForeColor = Color.Green;
                                cell.ToolTipText = "Sent band & mode";
                                break;
                            case QslStatus.SentBand:
                                cell.Style.BackColor = Color.Yellow;
                                cell.Style.ForeColor = Color.Black;
                                cell.ToolTipText = "Sent band only";
                                break;
                            case QslStatus.SentCountry:
                                cell.Style.BackColor = Color.Yellow;
                                cell.Style.ForeColor = Color.Red;
                                cell.ToolTipText = "Sent other band";
                                break;
                            case QslStatus.None:
                                cell.Style.BackColor = Color.DarkRed;
                                cell.Style.ForeColor = Color.Yellow;
                                cell.ToolTipText = "New country";
                                break;
                        }

                    break;
                }
            case "LoTW" when (lotwStatus == "Y" || lotwStatus == "V"):
                cell.Style.BackColor = Color.DarkGreen;
                cell.Style.ForeColor = Color.LimeGreen;
                break;
            case "RcvQsl" when (rcvQsl == "Y" || rcvQsl == "V"):
                cell.Style.BackColor = Color.DarkRed;
                cell.Style.ForeColor = Color.White;
                break;
            case "Meta":
                {
                    if (meta.Equals("P", StringComparison.OrdinalIgnoreCase) || meta.Equals("Print", StringComparison.OrdinalIgnoreCase))
                    {
                        cell.Style.BackColor = Color.MediumAquamarine;
                        cell.Style.ForeColor = Color.Black;
                    }
                    else if (meta.Equals("W", StringComparison.OrdinalIgnoreCase) || meta.Equals("Wait", StringComparison.OrdinalIgnoreCase))
                    {
                        cell.Style.BackColor = Color.Khaki;
                        cell.Style.ForeColor = Color.Black;
                    }

                    ColorizeCallSiblings(e.RowIndex, gridLog.Columns["Metadata"]!.Index);
                    break;
                }
        }
    }

    private void gridLog_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.ColumnIndex != gridLog.Columns[QslSentColName]!.Index || e.RowIndex < 0)
            return;

        e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

        var text = e.FormattedValue!.ToString();
        var textColor = Color.Black; //default color
        var bkgColor = text switch
        {
            "Q" => Color.Tomato,
            "Y" => Color.MediumAquamarine,
            "R" => Color.Khaki,
            _ => SystemColors.Window
        };

        using (var backgroundBrush = new SolidBrush(bkgColor))
            e.Graphics!.FillRectangle(backgroundBrush, e.CellBounds.X + 2, e.CellBounds.Y + 2, e.CellBounds.Width - 20, e.CellBounds.Height - 4);

        using (var textBrush = new SolidBrush(textColor))
            e.Graphics!.DrawString(text, e.CellStyle!.Font, textBrush, e.CellBounds.X + 2, e.CellBounds.Y + 2);

        e.Handled = true;
    }

    private void gridLog_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        var metaColIdx = gridLog.Columns["Metadata"]!.Index;
        var deliveryColIdx = gridLog.Columns[QslDeliveryColName]!.Index;
        var qslSentColIdx = gridLog.Columns[QslSentColName]!.Index;

        if (e.RowIndex == 0) return;

        if (e.ColumnIndex == metaColIdx)
        {
            var meta = gridLog.Rows[e.RowIndex].Cells[metaColIdx].Value?.ToString() ?? "";
            if (!meta.Equals("P", StringComparison.OrdinalIgnoreCase) && !meta.Equals("Print", StringComparison.OrdinalIgnoreCase) && !meta.Equals("W", StringComparison.OrdinalIgnoreCase) && !meta.Equals("Wait", StringComparison.OrdinalIgnoreCase))
            {
                gridLog.Rows[e.RowIndex].Cells[metaColIdx].Style = new DataGridViewCellStyle();
            }
        }
        else if (e.ColumnIndex == deliveryColIdx)
        {
            if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
            {
                var value = gridLog.Rows[e.RowIndex].Cells[deliveryColIdx].Value?.ToString() ?? "";
                ApplyValueToSiblings(e.RowIndex, deliveryColIdx, value, 1);
                ApplyValueToSiblings(e.RowIndex, deliveryColIdx, value, -1);
            }
        }
        else if (e.ColumnIndex == qslSentColIdx)
        {
            if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
            {
                var value = gridLog.Rows[e.RowIndex].Cells[qslSentColIdx].Value?.ToString() ?? "";
                ApplyValueToSiblings(e.RowIndex, qslSentColIdx, value, 1);
                ApplyValueToSiblings(e.RowIndex, qslSentColIdx, value, -1);
            }
        }
    }

    private Color _prevRowColor;
    private int _highlightedRowIndex = -1;
    private void gridLog_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return; // Ensure it's not the header row

        if (_highlightedRowIndex >= 0)
            gridLog.Rows[_highlightedRowIndex].DefaultCellStyle.BackColor = _prevRowColor;

        _highlightedRowIndex = e.RowIndex;

        _prevRowColor = gridLog.Rows[e.RowIndex].DefaultCellStyle.BackColor;
        gridLog.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
    }

    private void gridLog_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex == gridLog.Columns["revCol"]!.Index && e.RowIndex >= 0)
        {
            // Prevent the default mouse down behavior (selecting the cell)
            ((DataGridView)sender).CurrentCell = null;
        }
    }

    private void gridLog_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (e.ColumnIndex == gridLog.Columns[QslSentColName]!.Index || e.ColumnIndex == gridLog.Columns[QslDeliveryColName]!.Index)
        {
            gridLog.CurrentCell = gridLog.Rows[e.RowIndex].Cells[e.ColumnIndex];
            gridLog.BeginEdit(false);
            if (gridLog.EditingControl is ComboBox comboBox)
                comboBox.DroppedDown = true;
        }
        else if (e.ColumnIndex == gridLog.Columns["revCol"]!.Index)
        {
            ((LogGridModel)gridLog.Rows[e.RowIndex].DataBoundItem!).RevertChanges();
            gridLog.Rows[e.RowIndex].Cells["revCol"].Value = "";
            _dirtyRowsCount--;
            btnSave.Enabled = _dirtyRowsCount > 0;
        }
    }

    private void gridLog_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex < 0 || gridLog.Columns[e.ColumnIndex] is not DataGridViewLinkColumn) return;

        var url = $"https://www.qrz.com/db/{gridLog[e.ColumnIndex, e.RowIndex].Value}";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void gridLog_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is not ComboBox cb)
            return;

        cb.DrawItem -= comboBox_DrawItem;

        if (gridLog.CurrentCell!.ColumnIndex != gridLog.Columns[QslSentColName]!.Index)
        {
            cb.DrawMode = DrawMode.Normal;
            return;
        }

        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.DrawItem += comboBox_DrawItem;
    }

    private void gridLog_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        HandleContextMenu(e);
    }

    private void gridLog_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
    {
        if (e.RowIndex < 1) return;

        var callIdx = gridLog.Columns[CallColName]!.Index;
        var curCall = gridLog.Rows[e.RowIndex].Cells[callIdx].Value!.ToString();
        var prevCall = gridLog.Rows[e.RowIndex - 1].Cells[callIdx].Value!.ToString();
        if (curCall == prevCall) return;

        var startX = gridLog.RowHeadersVisible ? gridLog.RowHeadersWidth : 0;
        var endX = startX + gridLog.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) - 1;
        var rowTop = e.RowBounds.Top - 1;

        using var pen = new Pen(Color.Black, 1);
        e.Graphics.DrawLine(pen, startX, rowTop, endX, rowTop);
    }

    private void comboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox cb || e.Index < 0)
            return;

        // Set the color based on the item
        var text = cb.Items[e.Index]!.ToString();
        var textColor = text switch
        {
            "Q" => Color.Red,
            "Y" => Color.DarkGreen,
            "R" => Color.Yellow,
            _ => Color.Black
        };

        // draw the background (highlights selected item)
        e.DrawBackground();

        // draw the text
        using (Brush textBrush = new SolidBrush(textColor))
            e.Graphics.DrawString(text, e.Font!, textBrush, e.Bounds);

        e.DrawFocusRectangle();
    }

    private bool _firstDataBind = true;
    private void gridLog_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
        if (_firstDataBind)
        {
            _firstDataBind = false;
            return;
        }

        ConvertCallColumnToLink();
        AddCustomColumns();
        ConvertViaColumnToComboBox();
        if (ConvertQslSentColumnToComboBox()) //first grid load, returns false if column already exists
            ApplyGridStyle();
        ApplyRowStyles();

        foreach (DataGridViewRow row in gridLog.Rows)
        {
            if (row.DataBoundItem is LogGridModel model)
                model.Hydrated = true;
        }
    }
}
