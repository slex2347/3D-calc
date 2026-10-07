namespace PrintCalc3D;

/// <summary>
/// Параметры производительности в одном месте.
/// Расчёт цены — это несколько умножений (доли микросекунды), поэтому он НЕ раскидывается по потокам:
/// накладные расходы потоков были бы в тысячи раз больше самого расчёта.
/// Потоки реально используются там, где есть что распараллеливать: загрузка и разбор страниц магазинов.
/// </summary>
public static class PerfConfig
{
    public const int PhysicalCores = 4;     // физических ядер (для справки)
    public const int LogicalThreads = 8;    // логических потоков

    /// <summary>Сколько страниц магазинов грузить и разбирать одновременно (не больше числа потоков процессора).</summary>
    public static readonly int MaxParallel = Math.Max(1, Math.Min(LogicalThreads, Environment.ProcessorCount));

    /// <summary>Шаг обновления интерфейса: ~60 кадров в секунду (1000 / 60 ≈ 16 мс).</summary>
    public const int FrameMs = 16;

    /// <summary>
    /// Двойная буферизация всего окна целиком (WS_EX_COMPOSITED) — главное средство против мерцания
    /// при сворачивании/разворачивании. Если на каком-то ПК появятся артефакты отрисовки — поставьте false.
    /// </summary>
    public static readonly bool UseCompositedWindow = true;
}
