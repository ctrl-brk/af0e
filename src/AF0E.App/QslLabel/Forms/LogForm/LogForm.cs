using System.Collections;
using AF0E.DB;

namespace QslLabel.Forms.LogForm;

internal sealed partial class LogForm : Form
{
    private readonly string _value = null!;
#pragma warning disable CA2213 // Disposable fields should be disposed
    private readonly HrdDbContext _dbContext = null!;
#pragma warning restore CA2213
    private readonly string _mode;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    internal LogForm()
#pragma warning restore CS8618
    {
        InitializeComponent();
    }

    internal LogForm(string mode, string value, HrdDbContext dbContext) : this()
    {
        _mode = mode.ToUpperInvariant();
        _value = value;
        _dbContext = dbContext;
    }

    private void CountryForm_Load(object sender, EventArgs e)
    {
        lvLog.Columns.Add("Date", 100);
        lvLog.Columns.Add("Call", 100);
        lvLog.Columns.Add("Band", 50);
        lvLog.Columns.Add("Mode", 50);
        lvLog.Columns.Add("QSL sent", 80);
        lvLog.Columns.Add("QSL rcvd", 80);
        lvLog.Columns.Add("LTW rcvd", 65);

        var log = _mode == "COUNTRY" ?
            _dbContext.Log.Where(x => x.ColCountry == _value && x.ColQslSent != "I").OrderByDescending(x => x.ColTimeOn).ToList() :
            _dbContext.Log.Where(x => x.ColCall == _value && x.ColQslSent != "I").OrderByDescending(x => x.ColTimeOn).ToList();

        foreach (var q in log)
        {
            var lvi = new ListViewItem(q.ColTimeOn!.Value.ToString("yy-MM-dd HH:mm"));

            lvi.SubItems.Add(q.ColCall);
            lvi.SubItems.Add(q.ColBand);
            lvi.SubItems.Add(q.ColMode);

            switch (q.ColQslSent)
            {
                case "Y":
                case "R":
                    lvi.SubItems.Add($"{q.ColQslsdate?.ToString("yy-MM-dd")} ({(q.ColQslSent == "Y" ? q.ColQslSentVia : "R")})");
                    break;
                case "Q":
                    lvi.SubItems.Add("Q");
                    break;
                default:
                    lvi.SubItems.Add("");
                    break;
            }

            lvi.SubItems.Add(q.ColQslRcvd == "V" || q.ColQslRcvd == "Y" ? $"{q.ColQslrdate?.ToString("yy-MM-dd")} ({q.ColQslRcvdVia})" : "");
            lvi.SubItems.Add(q.ColLotwQslRcvd == "V" || q.ColLotwQslRcvd == "Y" ? q.ColLotwQslrdate?.ToString("yy-MM-dd") : "");

            lvLog.Items.Add(lvi);
        }
    }

    private void lvLog_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        if (lvLog.ListViewItemSorter is ListViewItemComparer sorter && sorter.Col == e.Column)
            sorter.Order = sorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        else
            lvLog.ListViewItemSorter = new ListViewItemComparer {Col = e.Column};

        lvLog.Sort();
    }

    private void lvCountryLog_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Escape)
            Close();
    }
}

internal sealed class ListViewItemComparer : IComparer
{
#pragma warning disable IDE1006
    public int Col;
#pragma warning restore IDE1006
    public SortOrder Order = SortOrder.Ascending;

    public int Compare(object? a, object? b)
    {
        var ret = string.Compare(((ListViewItem)a!).SubItems[Col].Text, ((ListViewItem)b!).SubItems[Col].Text, StringComparison.OrdinalIgnoreCase);

        if (Order == SortOrder.Descending)
            ret *= -1;

        return ret;
    }
}
