using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AF0E.DB;
using Microsoft.EntityFrameworkCore;
using QslLabel.Forms;
using QslLabel.Labels;
using QslLabel.Models;

namespace QslLabel;

internal sealed partial class MainForm : Form
{
    private const string CallColName = "CallLink";
    private const string QslSentColName = "cbQslSentCol";
    private const string QslDeliveryColName = "cbQslDeliveryCol";
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed on closing")]
    private HrdDbContext _dbContext = null!;
    private BindingList<LogGridModel> _contacts = null!;
    private static readonly object _sQslDataSource = new[] { "Q", "N", "R", "Y", "I" };
    private static readonly object _viaDataSource = new[] { new { Name = "", Value = "" }, new { Name = "Direct", Value = "D" }, new { Name = "Bureau", Value = "B" }, new { Name = "Manager", Value = "M" }, new { Name = "Electronic", Value = "E" } }.ToList();
    private int _dirtyRowsCount;

    public MainForm()
    {
        InitializeComponent();
        AcceptButton = btnSearch;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        _dbContext = new HrdDbContext(AppSettings.ConnectionString);
        gridLog.SetDoubleBuffered();
        SetupContextMenu();
        tbCall.Focus();
    }

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
        btnMarkSent.Enabled = false;
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

    private void gridLog_SelectionChanged(object sender, EventArgs? e)
    {
        btnGenPdf.Enabled = gridLog.SelectedRows.Count > 0 && cmbTemplate.SelectedItem != null;
        btnMarkSent.Enabled = gridLog.SelectedRows.Count > 0;
    }

    private async Task DoSearch(bool analyze)
    {
        if (btnSave.Enabled && MessageBox.Show("Reload anyway?", "There are unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        int printCountB = 0, printCountD = 0;

        if (!analyze && string.IsNullOrEmpty(tbCall.Text))
            cbQueued.Checked = true;

        _highlightedRowIndex = 0;

        Cursor = Cursors.WaitCursor;

        var contacts = await _dbContext.Database.SqlQueryRaw<LogGridModel>(
            $"""
             exec GetQslLabelData
                  @Call={(string.IsNullOrEmpty(tbCall.Text) ? "null" : $"'{tbCall.Text}'")},
                  @QueuedOnly={(cbQueued.Checked ? 1 : 0)},
                  @Analyze={(analyze ? 1 : 0)},
                  @DxOnly={(cbDxOnly.Checked ? 1 : 0)}
             """
        ).ToListAsync();

        if (contacts.Count == 0)
        {
            Cursor = Cursors.Default;
            MessageBox.Show("Not in log!", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return;
        }

        if (analyze)
            contacts = [.. contacts.OrderBy(x => x.Country).ThenBy(x => x.Call).ThenByDescending(x => x.sQSL).ThenByDescending(x => x.UTC)];

        foreach (var q in contacts)
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

        _contacts = new BindingList<LogGridModel>(contacts);
        _contacts.ListChanged += ContactChanged;
        gridLog.DataSource = _contacts;
        lblStatus.Text = $"{_contacts.Count:#,##0} ({_contacts.DistinctBy(x => x.Call).Count():#,##0} calls) qso's";
        if (printCountB + printCountD > 0)
            lblStatus.Text += $". {printCountB + printCountD:#,##0} labels: {printCountB:#,##0} buro, {printCountD:#,##0} direct";
        Cursor = Cursors.Default;
        //styles applied in gridLog_DataBindingComplete()
    }

    private void gridLog_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
        if (!gridLog.IsCurrentCellDirty) return;

        if (gridLog.CurrentCell is DataGridViewComboBoxCell)
            gridLog.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void ContactChanged(object? sender, ListChangedEventArgs e)
    {
        gridLog.Rows[e.NewIndex].Cells["revCol"].Style.Font = new Font("Wingdings 3", 11);
        gridLog.Rows[e.NewIndex].Cells["revCol"].Value = "Q"; //revert symbol
        _dirtyRowsCount++;
        btnSave.Enabled = true;
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

            var meta = row.Cells[metaIdx].Value?.ToString() ?? "";

            if (meta.Equals("P", StringComparison.OrdinalIgnoreCase) || meta.Equals("Print", StringComparison.OrdinalIgnoreCase))
            {
                row.Selected = true;
                selectionChanged = true;

                var call = row.Cells[callIdx].Value!.ToString();
                while (idx < gridLog.Rows.Count && gridLog.Rows[++idx].Cells[callIdx].Value!.ToString() == call)
                {
                    gridLog.Rows[idx].Selected = true;
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

        LabelCreator.CreateLabels(from DataGridViewRow row in gridLog.SelectedRows select (LogGridModel)row.DataBoundItem!, templateType, int.Parse(cmbStartLabelNum.SelectedItem!.ToString()!), printDeliveryMethod, FileType.PDF, fileName);
    }

    private void btnMarkSent_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in gridLog.SelectedRows)
        {
            ((LogGridModel)row.DataBoundItem!).sQSL = "Y";
        }
    }

    private void cbViewMyLocation_Click(object sender, EventArgs e)
    {
        gridLog.Columns["MyGrid"]!.Visible = gridLog.Columns["MyState"]!.Visible = gridLog.Columns["MyCity"]!.Visible = gridLog.Columns["MyCounty"]!.Visible = cbViewMyLocation.Checked;
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

    private void gridLog_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
    {
        string txt;

        switch (e.Column.Name)
        {
            case "lQSL": txt = "LoTW QSL Status"; break;
            case QslSentColName: txt = "QSL Status"; break;
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
                var delivery = gridLog.Rows[e.RowIndex].Cells[deliveryColIdx].Value?.ToString() ?? "";
                ApplyDeliveryToSiblings(e.RowIndex, deliveryColIdx, delivery, 1);
                ApplyDeliveryToSiblings(e.RowIndex, deliveryColIdx, delivery, -1);
            }
        }
    }

    private void ApplyDeliveryToSiblings(int rowIdx, int colIdx, string value, int step)
    {
        var callIdx = gridLog.Columns[CallColName]!.Index;
        var call =  gridLog.Rows[rowIdx].Cells[callIdx].Value!.ToString();

        rowIdx += step;

        while (rowIdx < gridLog.RowCount && rowIdx >= 0)
        {
            if (gridLog.Rows[rowIdx].Cells[callIdx].Value!.ToString() != call) break;
            gridLog.Rows[rowIdx].Cells[colIdx].Value = value;
            rowIdx += step;
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

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        var close = true;

        if (btnSave.Enabled)
            close = MessageBox.Show("Exit anyway?", "There are unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

        if (close)
        {
            _dbContext.Dispose();
            _dbContext = null!;
        }
        else
            e.Cancel = true;
    }
}
