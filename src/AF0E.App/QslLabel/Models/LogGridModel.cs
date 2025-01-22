using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace QslLabel.Models;

internal class LogGridModel : INotifyPropertyChanged
{
    [NotMapped]
    private Dictionary<string, object?> _OrigValues = [];

    public string Call { get; init; } = null!;
    public DateTime UTC { get; init; }
    public string? sQSL { get; init; } // Y, N

    private string? _via;
    public string? Via //qsl sent via (B, D, M, etc...)
    {
        get => _via;
        set => SetField(ref _via, value, nameof(Via));
        }

    private string? _manager;
    public string? Manager
    {
        get => _manager;
        set => SetField(ref _manager, value, nameof(Manager));
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
    public int ID { get; init; }

    public void RevertChanges()
    {
        if (_OrigValues.TryGetValue(nameof(Via), out var str))
            _via = str as string;
        if (_OrigValues.TryGetValue(nameof(Manager), out str))
            _manager = str as string;
        if (_OrigValues.TryGetValue(nameof(QslComment), out str))
            _qslComment = str as string;
        if (_OrigValues.TryGetValue(nameof(Comment), out str))
            _comment = str as string;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        _OrigValues.TryAdd(propertyName, field);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
