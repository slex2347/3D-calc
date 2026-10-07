namespace PrintCalc3D;

/// <summary>
/// Объединяет частые обновления интерфейса: за один «кадр» (PerfConfig.FrameMs ≈ 16 мс, то есть 60 раз в секунду)
/// выполняется только последнее поставленное действие. В простое таймер выключен и процессор не загружает.
/// </summary>
public sealed class FrameCoalescer : IDisposable
{
    readonly System.Windows.Forms.Timer _timer = new() { Interval = PerfConfig.FrameMs };
    Action _pending;

    public FrameCoalescer()
    {
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            var a = _pending;
            _pending = null;
            a?.Invoke();
        };
    }

    public void Post(Action action)
    {
        _pending = action;
        if (!_timer.Enabled) _timer.Start();
    }

    public void Dispose() => _timer.Dispose();
}
