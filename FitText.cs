namespace PrintCalc3D;

public enum TextRole { Normal, Muted, Accent }

/// <summary>
/// Однострочный текст, который сам подбирает размер шрифта под ширину элемента:
/// если не влезает — шрифт уменьшается (но не меньше minSize), а затем добавляется «…».
/// Высота строки всегда считается по базовому шрифту, поэтому вёрстка не «прыгает».
/// Цвет берётся из темы (Theme), так что тёмная/светлая тема подхватывается сама.
/// </summary>
public class FitText : Control
{
    const TextFormatFlags Flags =
        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix |
        TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;

    static readonly Size Huge = new(int.MaxValue, int.MaxValue);

    readonly Font _baseFont;
    readonly float _minSize;
    readonly bool _alignRight;
    string _text = "";
    TextRole _role;
    Font _fitted;
    int _fittedWidth = -1;

    public FitText(Font baseFont, TextRole role = TextRole.Normal, float minSize = 8f, bool alignRight = false)
    {
        _baseFont = baseFont;
        _role = role;
        _minSize = minSize;
        _alignRight = alignRight;

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.Transparent;
        TabStop = false;
        AutoSize = true;
        Margin = new Padding(0);
    }

    public override string Text
    {
        get => _text;
        set
        {
            value ??= "";
            if (value == _text) return;
            _text = value;
            _fittedWidth = -1;
            Invalidate();
        }
    }

    public TextRole Role
    {
        get => _role;
        set { _role = value; Invalidate(); }
    }

    // Ширина не навязывается (1 px) — элемент занимает столько, сколько даст ячейка таблицы.
    // Высота — одна строка базового шрифта.
    public override Size GetPreferredSize(Size proposedSize)
    {
        int h = TextRenderer.MeasureText("Ag", _baseFont, Huge, Flags).Height;
        return new Size(1, h + 2);
    }

    protected override void OnResize(EventArgs e)
    {
        _fittedWidth = -1;
        base.OnResize(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_text.Length == 0 || ClientSize.Width <= 0) return;

        EnsureFont();
        Color c = _role == TextRole.Muted ? Theme.Muted
                : _role == TextRole.Accent ? Theme.Accent
                : Theme.Text;
        var flags = Flags | (_alignRight ? TextFormatFlags.Right : TextFormatFlags.Left);
        TextRenderer.DrawText(e.Graphics, _text, _fitted, ClientRectangle, c, flags);
    }

    void EnsureFont()
    {
        int w = ClientSize.Width;
        if (_fitted != null && _fittedWidth == w) return;
        _fitted?.Dispose();
        _fitted = Fit(_baseFont, _text, w, _minSize);
        _fittedWidth = w;
    }

    /// <summary>Самый большой шрифт (не больше базового и не меньше minSize), при котором текст влезает в width.</summary>
    static Font Fit(Font baseFont, string text, int width, float minSize)
    {
        if (string.IsNullOrEmpty(text) || width <= 0)
            return (Font)baseFont.Clone();

        int w0 = TextRenderer.MeasureText(text, baseFont, Huge, Flags).Width;
        if (w0 <= width)
            return (Font)baseFont.Clone();

        // Первое приближение по пропорции, затем 1–2 шага уточнения.
        float size = Math.Max(minSize, baseFont.Size * width / w0);
        var f = new Font(baseFont.FontFamily, size, baseFont.Style);
        int guard = 0;
        while (size > minSize && guard++ < 12 &&
               TextRenderer.MeasureText(text, f, Huge, Flags).Width > width)
        {
            f.Dispose();
            size = Math.Max(minSize, size - 0.5f);
            f = new Font(baseFont.FontFamily, size, baseFont.Style);
        }
        return f;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _fitted?.Dispose();
        base.Dispose(disposing);
    }
}
