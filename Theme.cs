using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PrintCalc3D;

/// <summary>Цвета, переключение тёмной/светлой темы и применение темы к окнам.</summary>
public static class Theme
{
    public static bool Dark = true;

    // Фирменный зелёный Bambu Studio
    public static readonly Color Accent      = Color.FromArgb(0, 174, 66);
    public static readonly Color AccentHover = Color.FromArgb(31, 196, 92);
    public static readonly Color AccentDown  = Color.FromArgb(0, 143, 55);

    public static Color Bg     => Dark ? Color.FromArgb(24, 25, 27)    : Color.FromArgb(240, 242, 245);
    public static Color Card   => Dark ? Color.FromArgb(38, 40, 43)    : Color.White;
    public static Color Input  => Dark ? Color.FromArgb(28, 29, 31)    : Color.FromArgb(244, 246, 248);
    public static Color Border => Dark ? Color.FromArgb(68, 71, 76)    : Color.FromArgb(214, 218, 223);
    public static Color Text   => Dark ? Color.FromArgb(238, 239, 241) : Color.FromArgb(32, 34, 38);
    public static Color Muted  => Dark ? Color.FromArgb(150, 154, 161) : Color.FromArgb(105, 110, 119);
    public static Color Hover  => Dark ? Color.FromArgb(58, 61, 66)    : Color.FromArgb(230, 233, 237);

    public static GraphicsPath Round(RectangleF r, float radius)
    {
        float d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void Apply(Control c)
    {
        switch (c)
        {
            case Form f:
                f.BackColor = Bg;
                f.ForeColor = Text;
                break;
            case Card cd: cd.Invalidate(); break;
            case BambuButton b: b.Invalidate(); break;
            case InputBox ib: ib.Invalidate(); break;
            case ComboBox cb:
                cb.FlatStyle = FlatStyle.Flat;
                cb.BackColor = Input;
                cb.ForeColor = Text;
                break;
            case TextBox tb:
                tb.BackColor = Input;
                tb.ForeColor = Text;
                break;
            case DataGridView dg:
                dg.EnableHeadersVisualStyles = false;
                dg.BackgroundColor = Card;
                dg.GridColor = Border;
                dg.BorderStyle = BorderStyle.None;
                dg.ColumnHeadersDefaultCellStyle.BackColor = Input;
                dg.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
                dg.ColumnHeadersDefaultCellStyle.SelectionBackColor = Input;
                dg.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
                dg.DefaultCellStyle.BackColor = Card;
                dg.DefaultCellStyle.ForeColor = Text;
                dg.DefaultCellStyle.SelectionBackColor = Accent;
                dg.DefaultCellStyle.SelectionForeColor = Color.White;
                dg.RowHeadersVisible = false;
                break;
            case Label l:
                l.BackColor = Color.Transparent;
                l.ForeColor = "muted".Equals(l.Tag) ? Muted
                            : "accent".Equals(l.Tag) ? Accent
                            : Text;
                break;
            case CheckBox ck:
                ck.BackColor = Color.Transparent;
                ck.ForeColor = Text;
                break;
            case Panel:   // включая TableLayoutPanel и FlowLayoutPanel
                c.BackColor = Color.Transparent;
                break;
        }
        foreach (Control ch in c.Controls) Apply(ch);
        c.Invalidate();
    }

    // Тёмная/светлая рамка заголовка окна (Windows 10 1809+ / Windows 11)
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void SetTitleBar(IntPtr hwnd)
    {
        try
        {
            int v = Dark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, 20, ref v, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, 19, ref v, sizeof(int));
        }
        catch { /* старая Windows — просто стандартный заголовок */ }
    }
}
