using System.Drawing.Drawing2D;

namespace PrintCalc3D;

/// <summary>Скруглённая карточка-подложка (с двойной буферизацией — без мерцания).</summary>
public class Card : Panel
{
    public Card()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Theme.Bg);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 14);
        using var fill = new SolidBrush(Theme.Card);
        using var pen = new Pen(Theme.Border);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }
}
