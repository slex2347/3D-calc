namespace PrintCalc3D;

/// <summary>
/// Главное окно: собирает панели (HeaderPanel, InputsPanel, ResultsPanel) и содержит логику —
/// расчёт по кнопке, обновление цены принтера, напоминание об индексации тарифа.
/// Внешний вид правится в UiConfig.cs и в самих панелях.
/// </summary>
public class MainForm : Form
{
    readonly AppSettings S = AppSettings.Load();
    readonly FrameCoalescer _frame = new();

    HeaderPanel _header;
    InputsPanel _inputs;
    ResultsPanel _results;
    System.Windows.Forms.Timer _timer;
    bool _indexationPrompted;
    bool _calculated;   // был ли хотя бы один расчёт (тогда смена настроек/цены принтера пересчитывает результат)

    public MainForm()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = UiText.WindowTitle;
        MinimumSize = UiConfig.WindowMinSize;
        Size = UiConfig.WindowSize;
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiConfig.Body;
        Padding = new Padding(UiConfig.WindowPadding);

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;

        Theme.Dark = S.DarkTheme;
        BuildUi();
        AcceptButton = _inputs.CalculateButton;   // Enter в любом поле = «Рассчитать»
        Theme.Apply(this);
        UpdatePrinterLabel();
        _inputs.UpdateMassHint(S.PlasticExtraPercent);

        Shown += async (_, _) => await OnStart();
    }

    /// <summary>
    /// WS_EX_COMPOSITED: всё окно рисуется целиком в буфере и выводится одним кадром —
    /// главное средство от мерцания при сворачивании/разворачивании и изменении размера.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            if (PerfConfig.UseCompositedWindow) cp.ExStyle |= 0x02000000;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.SetTitleBar(Handle);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer?.Stop();
        _frame.Dispose();
        base.OnFormClosed(e);
    }

    // ------------------------------------------------------------------ Сборка окна

    void BuildUi()
    {
        _header = new HeaderPanel();
        _header.RefreshClicked += async (_, _) => await RefreshPrice(true);
        _header.SettingsClicked += (_, _) => OpenSettings();

        var body = new BufferedTable
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Padding = new Padding(0, UiConfig.BodyTopGap, 0, 0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, UiConfig.LeftColumnPercent));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, UiConfig.RightColumnPercent));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _inputs = new InputsPanel(S) { Margin = new Padding(0, 0, UiConfig.CardGap, 0) };
        _inputs.Changed += (_, _) => _frame.Post(() => _inputs.UpdateMassHint(S.PlasticExtraPercent));
        _inputs.CalculateClicked += (_, _) => DoCalculate();

        _results = new ResultsPanel { Margin = new Padding(UiConfig.CardGap, 0, 0, 0) };

        body.Controls.Add(_inputs, 0, 0);
        body.Controls.Add(_results, 1, 0);

        Controls.Add(body);      // Fill добавляем первым
        Controls.Add(_header);   // Top — после него
    }

    // ------------------------------------------------------------------ Расчёт (по кнопке)

    void DoCalculate()
    {
        double hours = _inputs.Hours;
        double grams = _inputs.Grams;
        double orders = _inputs.Orders;

        var r = Calculator.Calc(S, hours, grams, _inputs.PricePerKg, orders);
        _results.Display(S, r, hours, grams, orders);
        _inputs.UpdateMassHint(S.PlasticExtraPercent);
        _inputs.MarkFresh();
        _calculated = true;
    }

    // ------------------------------------------------------------------ Настройки

    void OpenSettings()
    {
        using var f = new SettingsForm(S);
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            Theme.Dark = S.DarkTheme;
            Theme.Apply(this);
            Theme.SetTitleBar(Handle);
            _inputs.SyncSalary();
            UpdatePrinterLabel();
            _inputs.UpdateMassHint(S.PlasticExtraPercent);
            if (_calculated) DoCalculate();
            if (!S.UseManualPrice) _ = RefreshPrice(false);
        }
        else
        {
            // цена могла обновиться кнопкой проверки внутри настроек
            UpdatePrinterLabel();
            if (_calculated) DoCalculate();
        }
    }

    // ------------------------------------------------------------------ Запуск / цена принтера

    async Task OnStart()
    {
        if (!S.ElectricityConfigured)
        {
            MessageBox.Show(this,
                "Первый запуск. Укажите тариф на электроэнергию и остальные параметры — это делается один раз.",
                "Первоначальная настройка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenSettings();
        }
        else
        {
            CheckIndexation();
        }

        await RefreshPrice(false);

        // Раз в 6 часов; в простое никакой нагрузки.
        _timer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 };
        _timer.Tick += async (_, _) => { await RefreshPrice(false); CheckIndexation(); };
        _timer.Start();
    }

    void CheckIndexation()
    {
        if (_indexationPrompted || !S.ElectricityConfigured) return;
        if ((DateTime.Now - S.ElectricityUpdated).TotalDays < 182) return;
        _indexationPrompted = true;
        MessageBox.Show(this,
            $"Тариф на электроэнергию ({S.ElectricityTariff:0.00} ₽/кВт·ч) не обновлялся с " +
            $"{S.ElectricityUpdated:dd.MM.yyyy}. Прошло больше полугода — проверьте актуальный тариф " +
            "и скорректируйте его в настройках.",
            "Индексация тарифа", MessageBoxButtons.OK, MessageBoxIcon.Information);
        OpenSettings();
    }

    async Task RefreshPrice(bool manualClick)
    {
        if (S.UseManualPrice)
        {
            if (manualClick)
                MessageBox.Show(this, "Включена ручная цена принтера (Настройки). Автообновление отключено.",
                    "Цена принтера", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _header.SetStatus("Обновляю цену принтера…");
        var (ok, msg) = await PriceService.UpdateAsync(S);
        UpdatePrinterLabel();
        if (_calculated) DoCalculate();
        if (manualClick)
        {
            MessageBox.Show(this,
                (ok ? "Цена обновлена.\n\n" : "Не удалось получить цену. Используется последнее известное значение.\n\n") + msg,
                "Цена принтера", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }

    void UpdatePrinterLabel()
    {
        string status = S.UseManualPrice
            ? "Цена задана вручную"
            : S.PriceUpdated.HasValue
                ? "Автообновление: " + S.PriceUpdated.Value.ToString("dd.MM.yyyy HH:mm")
                : "Значение по умолчанию — укажите ссылки на магазины в настройках";

        _header.SetPrinter($"P2S Combo + AMS 2 Pro:  {Fmt.Rub0(S.EffectivePrinterPrice)}", status);
    }
}
