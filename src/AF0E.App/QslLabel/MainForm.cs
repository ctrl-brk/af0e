using Microsoft.EntityFrameworkCore;
using QslLabel.Labels;
using QslLabel.Models;

namespace QslLabel;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

internal partial class MainForm : Form
{
    private HrdDbContext _dbContext = null!;

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        _dbContext = new HrdDbContext(AppSettings.ConnectionString);
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

        btnSave.Enabled = gridLog.SelectedRows.Count > 0;
    }

    private async void tbCall_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar != 13)
            return;

#pragma warning disable CS4014 // call not awaited
        DoSearch();
#pragma warning restore CS4014
        tbCall.SelectAll();
    }

    private async void btnSearch_Click(object sender, EventArgs e)
    {
        await DoSearch();
    }

    private void gridLog_SelectionChanged(object sender, EventArgs e)
    {
        btnSave.Enabled = gridLog.SelectedRows.Count > 0 && cmbTemplate.SelectedItem != null;
    }

    private async Task DoSearch()
    {
        IQueryable<HrdLog> l;

        if (!string.IsNullOrEmpty(tbCall.Text))
        {
            l = cbQueued.Checked
                ? _dbContext.Log.Where(x => x.ColCall == tbCall.Text && x.ColQslSent == "Q")
                : _dbContext.Log.Where(x => x.ColCall == tbCall.Text);
        }
        else
        {
            //select queued and all non-sent qsl qsos for all queued call signs
            cbQueued.Checked = true;
            var qsos = await _dbContext.Log.Where(x => x.ColQslSent == "Q").Select(x => x.ColCall).ToListAsync();
            l = _dbContext.Log.Where(x => x.ColQslSent != "Y" && qsos.Contains(x.ColCall));
        }

        var contacts = await l
            .Include(x => x.PotaContacts)
            .ThenInclude(x => x.Activation)
            .ThenInclude(x => x.Park)
            .OrderBy(x => x.ColCall)
            .ThenByDescending(x => x.ColTimeOn)
            .Select(x => new LogGridModel
            {
                ID = x.ColPrimaryKey,
                UTC = x.ColTimeOn!.Value,
                Call = x.ColCall!.ToUpperInvariant(),
                Mode = x.ColMode!,
                RST = x.ColRstSent,
                Band = x.ColBand!,
                ParkNum = x.PotaContacts.Count > 0 ? x.PotaContacts.First().Activation.Park.ParkNum : "",
                POTA = x.PotaContacts.Count > 0 ? $"{x.PotaContacts.First().Activation.Park.ParkNum} - {x.PotaContacts.First().Activation.Park.ParkName}" : "",
                P2P = x.PotaContacts.Count > 0 ? x.PotaContacts.First().P2P : "",
                Sat = x.ColSatName,
                sQSL = x.ColQslSent,
                Via = x.ColUserDefined1,
                QslComment = x.ColUserDefined2,
                rQSL = x.ColQslRcvd,
                Name = x.ColName,
                Country = x.ColCountry,
                Comment = x.ColComment,
                MyGrid = string.IsNullOrWhiteSpace(x.ColMyGridsquare) ? "" : x.ColMyGridsquare.Substring(0, 6),
                MyState = x.ColMyState,
                MyCity = x.ColMyCity,
                MyCounty = x.ColMyCnty
            })
            .ToListAsync();

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

        gridLog.DataSource = contacts;
        //styles applied in the gridLog_DataBindingComplete()
    }

    private void btnSave_Click(object sender, EventArgs e)
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
    private void ChangeCallToLinkColumn()
    {
        DataGridViewColumn column = gridLog.Columns["Call"]!;

        DataGridViewLinkColumn linkColumn = new()
        {
            Name = column.Name,
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

    private void ApplyGridStyle()
    {
        gridLog.CellBorderStyle = DataGridViewCellBorderStyle.SingleVertical;
        gridLog.Columns["UTC"]!.DefaultCellStyle.Format = "yy-MM-dd HH:mm:ss";
        gridLog.Columns["UTC"]!.Width = 105;
        gridLog.Columns["sQSL"]!.Width = gridLog.Columns["rQSL"]!.Width = gridLog.Columns["Mode"]!.Width = gridLog.Columns["Band"]!.Width = gridLog.Columns["Mhz"]!.Width = gridLog.Columns["RST"]!.Width = 40;

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

    private void gridLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (gridLog.Columns[e.ColumnIndex].Name == "sQSL" && e.Value!.ToString() == "Q")
            e.CellStyle.ForeColor = Color.Firebrick;

        if (gridLog.Columns[e.ColumnIndex].Name == "rQSL" && e.Value!.ToString() == "Y")
            e.CellStyle.ForeColor = Color.DarkGreen;
    }

    private Color _prevRowColor;
    private void gridLog_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return; // Ensure it's not the header row

        _prevRowColor = gridLog.Rows[e.RowIndex].DefaultCellStyle.BackColor;
        gridLog.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
    }

    private void gridLog_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            gridLog.Rows[e.RowIndex].DefaultCellStyle.BackColor = _prevRowColor;
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

    private bool _firstDataBind = true;
    private void gridLog_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
    {
        if (!_firstDataBind)
        {
            _firstDataBind = false;
            return;
        }

        ChangeCallToLinkColumn();
        ApplyGridStyle();
    }
}
