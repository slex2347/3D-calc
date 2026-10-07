namespace PrintCalc3D;

/// <summary>TableLayoutPanel с двойной буферизацией (обычный мерцает при перерисовке).</summary>
public class BufferedTable : TableLayoutPanel
{
    public BufferedTable()
    {
        DoubleBuffered = true;
    }
}
