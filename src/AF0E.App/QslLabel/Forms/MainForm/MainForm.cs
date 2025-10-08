using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using AF0E.DB;
using QslLabel.Forms;
using QslLabel.Models;

namespace QslLabel;

internal sealed partial class MainForm : Form
{
    private const string CallColName = "CallLink";
    private const string QslSentColName = "cbQslSentCol";
    private const string QslDeliveryColName = "cbQslDeliveryCol";
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed on closing")]
    private HrdDbContext _dbContext = null!;
    private List<LogGridModel> _contactsFromDb = null!;
    private BindingList<LogGridModel> _contactsList = null!;
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
