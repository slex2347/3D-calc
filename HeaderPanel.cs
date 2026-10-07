namespace PrintCalc3D;

/// <summary>
/// Верхняя шапка: название, цена принтера, статус обновления и две кнопки.
/// Все три строки текста — FitText: они сами уменьшают шрифт, если окно узкое, поэтому не срезаются.
/// Высота шапки определяется содержимым (минимум — UiConfig.HeaderMinHeight).
/// </summary>
public class HeaderPanel : Card
{
    readonly FitText _title;
    readonly FitText _price;
    readonly FitText _status;

    public event EventHandler RefreshClicked;
    public event EventHandler SettingsClicked;

    public HeaderPanel()
    {
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(0, UiConfig.HeaderMinHeight);

        // внешняя таблица: [тексты | кнопка | кнопка]
        var outer = new BufferedTable
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 1,
            Padding = UiConfig.HeaderPadding
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // три строки текста друг под другом
        var info = new BufferedTable
        {
            Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 3,
            Margin = new Padding(0, 0, 12, 0)
        };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _title = new FitText(UiConfig.Title, TextRole.Normal, UiConfig.FitMinFont)
        {
            Text = UiText.AppTitle, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4)
        };
        _price = new FitText(UiConfig.StrongValue, TextRole.Normal, UiConfig.FitMinFont)
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 2)
        };
        _status = new FitText(UiConfig.Small, TextRole.Muted, UiConfig.FitMinFont)
        {
            Dock = DockStyle.Fill, Margin = new Padding(0)
        };
        info.Controls.Add(_title, 0, 0);
        info.Controls.Add(_price, 0, 1);
        info.Controls.Add(_status, 0, 2);

        var btnRefresh = new BambuButton
        {
            Text = UiText.BtnRefresh, Primary = false, Width = UiConfig.ButtonRefreshWidth,
            Anchor = AnchorStyles.None, Margin = new Padding(8, 3, 8, 3)
        };
        btnRefresh.Click += (_, _) => RefreshClicked?.Invoke(this, EventArgs.Empty);

        var btnSettings = new BambuButton
        {
            Text = UiText.BtnSettings, Width = UiConfig.ButtonSettingsWidth,
            Anchor = AnchorStyles.None, Margin = new Padding(8, 3, 0, 3)
        };
        btnSettings.Click += (_, _) => SettingsClicked?.Invoke(this, EventArgs.Empty);

        outer.Controls.Add(info, 0, 0);
        outer.Controls.Add(btnRefresh, 1, 0);
        outer.Controls.Add(btnSettings, 2, 0);
        Controls.Add(outer);
    }

    /// <summary>Строка с ценой принтера и строка статуса под ней.</summary>
    public void SetPrinter(string priceLine, string status)
    {
        _price.Text = priceLine;
        _status.Text = status;
    }

    public void SetStatus(string status) => _status.Text = status;
}
