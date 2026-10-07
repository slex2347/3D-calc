namespace PrintCalc3D;

/// <summary>
/// Правая карточка с результатами. Вверху две крупные плитки рядом: «Цена для клиента» и «Себестоимость»;
/// ниже — ставка за час, разбор заказа и окупаемость.
/// Только отображает числа — считает Calculator (Models.cs).
/// </summary>
public class ResultsPanel : Card
{
    readonly Dictionary<string, Label> _val = new();
    readonly Dictionary<string, Label> _cap = new();
    BufferedTable _table;
    FitText _price, _cost;
    Label _note;

    public ResultsPanel()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Build();

        SizeChanged += (_, _) =>
            _note.MaximumSize = new Size(Math.Max(120, ClientSize.Width - 60), 0);
    }

    // ------------------------------------------------------------------ Построение

    void Build()
    {
        _table = new BufferedTable
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = UiConfig.CardPadding
        };
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // подпись занимает всё свободное место
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // значение — по своей ширине

        // Две плитки рядом: цена для клиента | себестоимость
        var tiles = new BufferedTable
        {
            Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 2, Margin = new Padding(0)
        };
        tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        tiles.Controls.Add(TileCaption(UiText.ClientPrice), 0, 0);
        tiles.Controls.Add(TileCaption(UiText.CostTile), 1, 0);

        _price = new FitText(UiConfig.Big, TextRole.Accent, UiConfig.FitMinFont)
        {
            Text = "—", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 4)
        };
        _cost = new FitText(UiConfig.CostBig, TextRole.Normal, UiConfig.FitMinFont)
        {
            Text = "—", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4)
        };
        tiles.Controls.Add(_price, 0, 1);
        tiles.Controls.Add(_cost, 1, 1);

        _table.Controls.Add(tiles);
        _table.SetColumnSpan(tiles, 2);

        AddSection("s1", UiText.SecRate);
        AddRow("hpm", UiText.RowHoursMonth);
        AddRow("elec", UiText.RowElec);
        AddRow("salary", UiText.RowSalary);
        AddRow("amort", "Амортизация");
        AddRow("related", "Сопутствующие");
        AddRow("hour", UiText.RowHourTotal, true);

        AddSection("s2", UiText.SecOrder);
        AddRow("mass", UiText.RowMass);
        AddRow("plastic", UiText.RowPlastic);
        AddRow("machine", UiText.RowMachine);
        AddRow("cost", UiText.RowCost, true);
        AddRow("markup", UiText.RowMarkup);
        AddRow("price", "Цена с наценкой", true);

        AddSection("s3", UiText.SecPayback);
        AddRow("profit", UiText.RowProfit);
        AddRow("need", UiText.RowNeed);
        AddRow("payback", UiText.RowPayback, true);

        _note = new Label
        {
            Tag = "muted", AutoSize = true, Font = UiConfig.Small, Margin = new Padding(0, 14, 0, 0),
            Text = UiText.PaybackNote
        };
        _table.Controls.Add(_note);
        _table.SetColumnSpan(_note, 2);

        Controls.Add(_table);
    }

    static FitText TileCaption(string text) =>
        new(UiConfig.Body, TextRole.Muted, UiConfig.FitMinFont)
        {
            Text = text, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 2)
        };

    void AddSection(string key, string text)
    {
        var l = new Label
        {
            Text = text, Font = UiConfig.Section, AutoSize = true, Margin = new Padding(0, 16, 0, 4)
        };
        _cap[key] = l;
        _table.Controls.Add(l);
        _table.SetColumnSpan(l, 2);
    }

    void AddRow(string key, string caption, bool bold = false)
    {
        var c = new Label
        {
            Text = caption, Tag = bold ? null : "muted", AutoSize = true,
            Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 12, 4),
            Font = bold ? UiConfig.Strong : UiConfig.Body
        };
        var v = new Label
        {
            Text = "—", AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 4, 0, 4),
            Font = bold ? UiConfig.StrongValue : UiConfig.Body
        };
        _cap[key] = c;
        _val[key] = v;
        _table.Controls.Add(c);
        _table.Controls.Add(v);
    }

    // ------------------------------------------------------------------ Вывод результата

    // Меняем текст только если он действительно изменился — лишние перерисовки не нужны.
    static void Set(Label l, string text)
    {
        if (l.Text != text) l.Text = text;
    }

    public void Display(AppSettings s, CalcResult r, double hours, double grams, double orders)
    {
        bool has = hours > 0 || grams > 0;

        _table.SuspendLayout();   // одна перекладка вместо десятка
        try
        {
            string dash = "—";

            _price.Text = has ? Fmt.Rub0(r.Price) : dash;
            _cost.Text = has ? Fmt.Rub0(r.Cost) : dash;

            Set(_val["hpm"], $"{r.HoursPerMonth:0.#} ч");
            Set(_val["elec"], Fmt.Rub2(r.ElecPerHour));
            Set(_val["salary"], s.SalaryEnabled ? Fmt.Rub2(r.SalaryPerHour) : "отключена");
            Set(_cap["amort"], $"Амортизация ({s.PaybackMonths:0.#} мес.)");
            Set(_val["amort"], Fmt.Rub2(r.AmortPerHour));
            Set(_cap["related"], $"Сопутствующие ({s.RelatedPercent:0.#}% за {s.RelatedMonths:0.#} мес.)");
            Set(_val["related"], Fmt.Rub2(r.RelatedPerHour));
            Set(_val["hour"], Fmt.Rub2(r.HourRate));

            Set(_val["mass"], grams > 0 ? $"{r.MassWithExtra:0.#} г" : dash);
            Set(_val["plastic"], has ? Fmt.Rub2(r.PlasticCost) : dash);
            Set(_val["machine"], has ? Fmt.Rub2(r.MachineCost) : dash);
            Set(_val["cost"], has ? Fmt.Rub2(r.Cost) : dash);
            Set(_val["markup"], has ? Fmt.Rub2(r.Markup) : dash);
            Set(_cap["price"], $"Цена с наценкой {s.MarginPercent:0.#}%");
            Set(_val["price"], has ? Fmt.Rub0(r.Price) : dash);

            Set(_cap["s3"], $"Окупаемость за {s.PaybackMonths:0.#} мес.");
            Set(_val["profit"], has ? Fmt.Rub2(r.ProfitPerOrder) : dash);
            Set(_val["need"], has && !double.IsNaN(r.OrdersNeeded)
                ? $"{Math.Ceiling(r.OrdersNeeded):0} шт. / мес." : dash);
            Set(_val["payback"], has && !double.IsNaN(r.PaybackMonths)
                ? $"≈ {r.PaybackMonths:0.#} мес." : (orders > 0 ? dash : "укажите заказы/мес"));
        }
        finally
        {
            _table.ResumeLayout(true);
        }
    }
}
