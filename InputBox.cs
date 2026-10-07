using System.Drawing.Drawing2D;

namespace PrintCalc3D;

/// <summary>Поле ввода со скруглённой рамкой.</summary>
public class InputBox : Panel
{
    public readonly TextBox Tb = new();
    bool _focus;

    public event EventHandler ValueChanged;

    public InputBox()
    {
        DoubleBuffered = true;
        Height = UiConfig.InputHeight;
        Padding = new Padding(12, 9, 12, 4);
        Tb.BorderStyle = BorderStyle.None;
        Tb.Dock = DockStyle.Fill;
        Tb.Font = UiConfig.Input;
        Tb.Enter += (_, _) => { _focus = true; Invalidate(); };
        Tb.Leave += (_, _) => { _focus = false; Invalidate(); };
        Tb.TextChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(Tb);
    }

    public override string Text
    {
        get => Tb.Text;
        set => Tb.Text = value;
    }

    public double Number => Fmt.Num(Tb.Text);

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Theme.Card);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 9);
        using var fill = new SolidBrush(Theme.Input);
        using var pen = new Pen(_focus ? Theme.Accent : Theme.Border, _focus ? 1.6f : 1f);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }
}
