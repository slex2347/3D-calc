using System.Drawing.Drawing2D;

namespace PrintCalc3D;

/// <summary>Кнопка в стиле Bambu Studio: зелёная (основная) или контурная (вторичная).</summary>
public class BambuButton : Button
{
    public bool Primary { get; set; } = true;
    bool _hover, _down;

    public BambuButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Height = UiConfig.ButtonHeight;
        Font = UiConfig.Button;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    // Цвет фона, на котором стоит кнопка (чтобы скруглённые углы сливались с ним)
    Color Surface()
    {
        for (Control p = Parent; p != null; p = p.Parent)
        {
            if (p is Card) return Theme.Card;
            if (p is Form) return Theme.Bg;
        }
        return Theme.Card;
    }

    protected override bool ShowFocusCues => false;
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Surface());

        Color fill = Primary
            ? (_down ? Theme.AccentDown : _hover ? Theme.AccentHover : Theme.Accent)
            : (_down ? Theme.Border : _hover ? Theme.Hover : Theme.Input);
        Color fore = Primary ? Color.White : Theme.Text;

        var rect = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
        using var path = Theme.Round(rect, 10);
        using var brush = new SolidBrush(fill);
        g.FillPath(brush, path);
        if (!Primary)
        {
            using var pen = new Pen(Theme.Border);
            g.DrawPath(pen, path);
        }
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
