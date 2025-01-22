using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AF0E.DB;
using Microsoft.EntityFrameworkCore;
using QslLabel.Labels;
using QslLabel.Models;

namespace QslLabel;

internal partial class MainForm : Form
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed on closing")]
    private HrdDbContext _dbContext = null!;
    private BindingList<LogGridModel> _contacts = null!;
    private static object _sQslDataSource = new[] { "Q", "N", "R", "Y", "I" };
    private static object _viaDataSource = new[] { new { Name = "", Value = "" }, new { Name = "Direct", Value = "D" }, new { Name = "Bureau", Value = "B" }, new { Name = "Manager", Value = "M" }, new { Name = "Electronic", Value = "E" } }.ToList();
    private int _dirtyRowsCount;

    public MainForm()
    {
        InitializeComponent();
        AcceptButton = btnSearch;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        _dbContext = new HrdDbContext(AppSettings.ConnectionString);
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
        await DoSearch(false);
    }

    private async void btnAnalyze_Click(object sender, EventArgs e)
    {
        await DoSearch(true);
    }

    private void gridLog_SelectionChanged(object sender, EventArgs e)
    {
        btnGenPdf.Enabled = gridLog.SelectedRows.Count > 0 && cmbTemplate.SelectedItem != null;
    }

    private async Task DoSearch(bool analyze)
    {
        if (!analyze && string.IsNullOrEmpty(tbCall.Text))
            cbQueued.Checked = true;

        _highlightedRowIndex = 0;

        Cursor = Cursors.WaitCursor;

        var contacts = await _dbContext.Database.SqlQueryRaw<LogGridModel>(
            $@"exec GetQslLabelData
            @Call={(string.IsNullOrEmpty(tbCall.Text) ? "null" : $"'{tbCall.Text}'")},
            @QueuedOnly={(cbQueued.Checked ? 1 : 0)},
            @Analyze={(analyze ? 1 : 0)},
            @IncludeUS={(cbIncludeUS.Checked ? 1 : 0)}"
            ).ToListAsync();

        if (analyze)
            contacts = [.. contacts.OrderBy(x => x.Country).ThenBy(x => x.Call).ThenBy(x => x.Band).ThenBy(x => x.Mode).ThenByDescending(x => x.UTC)];

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
        }

        _contacts = new BindingList<LogGridModel>(contacts);
        _contacts.ListChanged += ContactChanged;
        gridLog.DataSource = _contacts;
        lblStatus.Text = $"{_contacts.Count:#,##0} records";
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

    private void btnGenPdf_Click(object sender, EventArgs e)
    {
        saveDlg.FileName = cmbTemplate.SelectedItem switch
        {
            "1.3 x 4" => "!QSL Labels 1.3x4", //exclamation sign is to keep it on top of the file list sorted by name
            "2.0 x 4" => "!QSL Labels 2x4",
        };

        if (!string.IsNullOrWhiteSpace(tbCall.Text))
            saveDlg.FileName += $" - {tbCall.Text}";

        if (saveDlg.ShowDialog() == DialogResult.Cancel)
            return;

        GeneratePdf(saveDlg.FileName);
    }

    private void GeneratePdf(string fileName)
    {
        TemplateType templateType = cmbTemplate.SelectedItem switch
        {
            "1.3 x 4" => TemplateType.OneAndThirdByFour,
            "2.0 x 4" => TemplateType.TwoByFour,
        };

        LabelCreator.CreateLabels(from DataGridViewRow row in gridLog.SelectedRows select (LogGridModel)row.DataBoundItem!, templateType, int.Parse(cmbStartLabelNum.SelectedItem!.ToString()!), FileType.PDF, fileName);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private void ConvertCallColumnToLink()
    {
        if (gridLog.Columns["CallLink"] != null)
            return;

        var column = gridLog.Columns["Call"]!;

        DataGridViewLinkColumn linkColumn = new()
        {
            Name = "CallLink",
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
        if (gridLog.Columns["cbQslSentCol"] != null)
            return false;

        var cbCol = new DataGridViewComboBoxColumn
        {
            Width = 50,
            HeaderText = "sQSL",
            Name = "cbQslSentCol",
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
        if (gridLog.Columns["cbQslViaCol"] != null)
            return false;

        var cbCol = new DataGridViewComboBoxColumn
        {
            Width = 80,
            HeaderText = "Via",
            Name = "cbQslViaCol",
            DataPropertyName = "Via",
            ValueMember = "Value",
            DisplayMember = "Name",
            DataSource = _viaDataSource,
        };

        var idx = gridLog.Columns["Via"]!.Index;
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
        gridLog.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical;
        gridLog.Columns["UTC"]!.DefaultCellStyle.Format = "yy-MM-dd HH:mm:ss";
        gridLog.Columns["UTC"]!.Width = 105;
        gridLog.Columns["rQSL"]!.Width = gridLog.Columns["lQSL"]!.Width = gridLog.Columns["Mode"]!.Width = gridLog.Columns["Band"]!.Width = gridLog.Columns["Mhz"]!.Width = gridLog.Columns["RST"]!.Width = 40;
        gridLog.Columns["CountryQslStatus"]!.Visible = false;
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
            case "cbQslSentCol": txt = "QSL Status"; break;
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
            case "Band": curCol = "B"; break;
            case "Mode": curCol = "M"; break;
            case "Country": curCol = "C"; break;
            case "lQSL": curCol = "LoTW"; break;
            default: return;
        }

        var statusIdx = gridLog.Columns["CountryQslStatus"]!.Index;
        var status = Enum.Parse<QslStatus>(gridLog.Rows[e.RowIndex].Cells[statusIdx].Value!.ToString()!);
        var lotwStatus = gridLog.Rows[e.RowIndex].Cells[gridLog.Columns["lQSL"]!.Index].Value!.ToString()!;
        var cell = gridLog.Rows[e.RowIndex].Cells[e.ColumnIndex]!;

        if (curCol == "B")
        {
            if (status == QslStatus.ReceivedBandMode || status == QslStatus.ReceivedBand)
            {
                cell.Style.BackColor = Color.DarkGreen;
                cell.Style.ForeColor = Color.LimeGreen;
            }
            else if (status == QslStatus.SentBandMode || status == QslStatus.SentBand)
            {
                cell.Style.BackColor = Color.Yellow;
                cell.Style.ForeColor = Color.Black;
            }
            else
            {
                cell.Style.BackColor = Color.DarkRed;
                cell.Style.ForeColor = Color.Yellow;
            }
        }
        else if (curCol == "M")
        {
            if (status == QslStatus.ReceivedBandMode)
            {
                cell.Style.BackColor = Color.DarkGreen;
                cell.Style.ForeColor = Color.LimeGreen;
            }
            else if (status == QslStatus.SentBandMode)
            {
                cell.Style.BackColor = Color.Yellow;
                cell.Style.ForeColor = Color.Black;
            }
            else
            {
                cell.Style.BackColor = Color.DarkRed;
                cell.Style.ForeColor = Color.Yellow;
            }
        }
        else if (curCol == "C")
        {
            var callIdx = gridLog.Columns["CallLink"]!.Index;
            var call = gridLog.Rows[e.RowIndex].Cells[callIdx].Value!.ToString()!;
            var specialCall = call.Length == 3; //only 1x1 for now

            if (specialCall)
            {
                cell.Style.BackColor = Color.DarkRed;
                cell.Style.ForeColor = Color.Yellow;
                cell.ToolTipText = "1x1";
            }
            else if (status == QslStatus.ReceivedBandMode)
            {
                cell.Style.BackColor = Color.DarkGreen;
                cell.Style.ForeColor = Color.LimeGreen;
                cell.ToolTipText = "Rcvd band & mode";
            }
            else if (status == QslStatus.ReceivedBand)
            {
                cell.Style.BackColor = Color.DarkGreen;
                cell.Style.ForeColor = Color.Yellow;
                cell.ToolTipText = "Rcvd band only";
            }
            else if (status == QslStatus.SentBandMode)
            {
                cell.Style.BackColor = Color.Yellow;
                cell.Style.ForeColor = Color.Black;
                cell.ToolTipText = "Sent band & mode";
            }
            else if (status == QslStatus.SentBand)
            {
                cell.Style.BackColor = Color.Yellow;
                cell.Style.ForeColor = Color.Red;
                cell.ToolTipText = "Sent band only";
            }
            else
            {
                cell.Style.BackColor = Color.DarkRed;
                cell.Style.ForeColor = Color.Yellow;
                cell.ToolTipText = "New country";
            }
        }
        else if (curCol == "LoTW" && (lotwStatus == "Y" || lotwStatus == "V"))
        {
            cell.Style.BackColor = Color.Yellow;
            cell.Style.ForeColor = Color.Black;
        }
    }

    private void gridLog_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.ColumnIndex != gridLog.Columns["cbQslSentCol"]!.Index || e.RowIndex < 0)
            return;

        e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

        var text = e.FormattedValue!.ToString();
        var textColor = Color.Black; //default color
        switch (text)
        {
            case "Q":
                textColor = Color.Red;
                break;
            case "Y":
                textColor = Color.DarkGreen;
                break;
        }

        // Draw the text in the specified color
        using (Brush textBrush = new SolidBrush(textColor))
        {
            e.Graphics!.DrawString(text, e.CellStyle!.Font, textBrush, e.CellBounds.X + 2, e.CellBounds.Y + 2);
        }

        e.Handled = true;
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

        if (e.ColumnIndex == gridLog.Columns["cbQslSentCol"]!.Index || e.ColumnIndex == gridLog.Columns["cbQslViaCol"]!.Index)
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
        if (e.ColumnIndex >= 0 && gridLog.Columns[e.ColumnIndex] is DataGridViewLinkColumn)
        {
            var url = $"https://www.qrz.com/db/{gridLog[e.ColumnIndex, e.RowIndex].Value}";

            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}");
            }
        }
    }

    private void gridLog_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is not ComboBox cb)
            return;

        cb.DrawItem -= comboBox_DrawItem;

        if (gridLog.CurrentCell!.ColumnIndex != gridLog.Columns["cbQslSentCol"]!.Index)
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

    private void comboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox cb || e.Index < 0)
            return;

        // Set the color based on the item
        var text = cb.Items[e.Index]!.ToString();
        var textColor = Color.Black;
        switch (text)
        {
            case "Q":
                textColor = Color.Red;
                break;
            case "Y":
                textColor = Color.DarkGreen;
                break;
        }

        // draw the background (highlights selected item)
        e.DrawBackground();

        // draw the text
        using (Brush textBrush = new SolidBrush(textColor))
        {
            e.Graphics.DrawString(text, e.Font!, textBrush, e.Bounds);
        }

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
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _dbContext?.Dispose();
        _dbContext = null!;
    }
}
