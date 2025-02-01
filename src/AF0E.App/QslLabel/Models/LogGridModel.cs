using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using AF0E.DB;
using Microsoft.EntityFrameworkCore;

namespace QslLabel.Models;

internal sealed class LogGridModel : INotifyPropertyChanged
{
    [NotMapped]
    private readonly Dictionary<string, object?> _origValues = [];
    [NotMapped]
    public bool Hydrated;

    public string Call { get; init; } = null!;
    public DateTime UTC { get; init; }

    private string? _sQsl;
    public string? sQSL // Y, N, R, Q, I
    {
        get => _sQsl;
        set
        {
            SetField(ref _sQsl, value, nameof(sQSL));
            if (value == "Y")
                sQSLDate = DateTime.UtcNow;
        }
    }

    private DateTime _sQslDate;
    public DateTime sQSLDate
    {
        set => SetField(ref _sQslDate, value, nameof(sQSLDate));
    }

    private string? _qslDeliveryMethod;
    public string? QslDeliveryMethod //qsl sent via buro, direct, manager... (B, D, M, ...)
    {
        get => _qslDeliveryMethod;
        set => SetField(ref _qslDeliveryMethod, value, nameof(QslDeliveryMethod));
    }

    private string? _qrzQslInfo;
    public string? QrzQslInfo
    {
        get => _qrzQslInfo;
        set => SetField(ref _qrzQslInfo, value, nameof(QrzQslInfo));
    }

    private string? _qslMgrCall;
    public string? QslMgrCall
    {
        get => _qslMgrCall;
        set => SetField(ref _qslMgrCall, value, nameof(QslMgrCall));
    }

    private string? _qslComment;
    public string? QslComment
    {
        get => _qslComment;
        set => SetField(ref _qslComment, value, nameof(QslComment));
    }

    public string? rQSL { get; init; }
    public string? lQSL { get; init; }
    [NotMapped]
    public string? Mhz { get; set; }
    public string Band { get; init; } = null!;
    public string Mode { get; init; } = null!;
    public string? RST { get; init; }
    public string Parks { get; init; } = null!;
    public string? Sat{ get; init; }
    public string? Name { get; init; }
    public string? Country { get; init; }
    public QslStatus CountryQslStatus { get; init; }

    private string? _siteComment;
    public string? SiteComment
    {
        get => _siteComment;
        set => SetField(ref _siteComment, value, nameof(SiteComment));
    }

    private string? _comment;
    public string? Comment
    {
        get => _comment;
        set => SetField(ref _comment, value, nameof(Comment));
    }

    public string? MyGrid { get; init; }
    public string? MyState { get; init; }
    public string? MyCity { get; init; }
    public string? MyCounty { get; init; }

    private string? _metadata;
    public string? Metadata
    {
        get => _metadata;
        set => SetField(ref _metadata, value, nameof(Metadata));
    }

    public int ID { get; init; }

    [NotMapped]
    public bool IsDirty => _origValues.Count > 0;

    public void RevertChanges()
    {
        if (_origValues.TryGetValue(nameof(sQSL), out var str))
        {
            _sQsl = str as string;
            if (_origValues.TryGetValue(nameof(sQSLDate), out str))
                _sQslDate = (str as DateTime?)!.Value;
        }
        if (_origValues.TryGetValue(nameof(QslDeliveryMethod), out str))
            _qslDeliveryMethod = str as string;
        if (_origValues.TryGetValue(nameof(QrzQslInfo), out str))
            _qrzQslInfo = str as string;
        if (_origValues.TryGetValue(nameof(QslMgrCall), out str))
            _qslMgrCall = str as string;
        if (_origValues.TryGetValue(nameof(QslComment), out str))
            _qslComment = str as string;
        if (_origValues.TryGetValue(nameof(SiteComment), out str))
            _siteComment = str as string;
        if (_origValues.TryGetValue(nameof(Comment), out str))
            _comment = str as string;
        if (_origValues.TryGetValue(nameof(Metadata), out str))
            _metadata = str as string;

        _origValues.Clear();
    }

    public void SaveChanges(HrdDbContext context)
    {
        if (!IsDirty) return;

        var qso = context.Log.AsTracking().First(x => x.ColPrimaryKey == ID);

        qso.ColQslSent = sQSL;
        if (_sQslDate != DateTime.MinValue)
            qso.ColQslsdate = _sQslDate;
        qso.ColQslSentVia = QslDeliveryMethod;
        qso.ColQslVia = QrzQslInfo;
        qso.QslMgrCall = QslMgrCall;
        qso.QslComment = QslComment;
        qso.SiteComment = SiteComment;
        qso.ColComment = Comment;
        qso.Metadata = Metadata;

        if (_origValues.TryGetValue(nameof(sQSL), out var orig_sQsl))
        {
            if ((string?)orig_sQsl != sQSL)
                qso.ColQslsdate = DateTime.UtcNow;
        }

        context.SaveChanges();

        _origValues.Clear();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string ToAdif() => $"<QSO_DATE:8>{UTC:yyyyMMdd} <TIME_ON:4>{UTC:HHmm} <CALL:{Call.Length}>{Call} <BAND:{Band.Length}>{Band} <MODE:{Mode.Length}>{Mode} <RST_SENT:{RST?.Length ?? 0}>{RST ?? ""} <EOR>";

    private void SetField<T>(ref T field, T value, string propertyName)
    {
        if (!Hydrated) {field = value; return;}

        if (EqualityComparer<T>.Default.Equals(field, value)) return;

        _origValues.TryAdd(propertyName, field);
        field = value;
        OnPropertyChanged(propertyName);
    }
}
