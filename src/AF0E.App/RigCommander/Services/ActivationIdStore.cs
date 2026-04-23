namespace RigCommander.Services;

public sealed class ActivationIdStore
{
    private readonly Lock _lock = new();
    private int? _value;

    public int? Get()
    {
        lock (_lock)
            return _value;
    }

    public void Set(int? value)
    {
        lock (_lock)
            _value = value;
    }
}
