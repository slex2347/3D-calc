using System.Globalization;

namespace PrintCalc3D;

/// <summary>Окно «Настройки»: тема, электричество, ЗП, формула, цена принтера и ссылки на магазины.</summary>
public class SettingsForm : Form
{
    readonly AppSettings _s;
    CheckBox chkDark, chkSalary, chkManual, chkConfirm;
    InputBox tbTariff, tbPower, tbSalary, tbDays, tbHours, tbPayback, tbRelPct, tbRelMonths, tbExtra, tbMargin, tbManual;
    TextBox tbUrls, tbLog;

    public SettingsForm(AppSettings s)
    {
        _s = s;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Настройки";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(640, 820);
        MinimumSize = new Size(560, 560);
        Font = UiConfig.Body;
        Padding = new Padding(14);
        ShowInTaskbar = false;

        var card = new Card { Dock = DockStyle.Fill, AutoScroll = true };
        var t = new BufferedTable
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(22, 8, 22, 16)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Section(t, "Внешний вид");
        chkDark = Check(t, "Тёмная тема", s.DarkTheme);

        Section(t, "Электричество");
        tbTariff = Field(t, "Тариф, ₽ за кВт·ч", s.ElectricityTariff);
        tbPower = Field(t, "Средняя мощность принтера и AMS, Вт", s.PrinterPowerW);
        t.Controls.Add(new Label
        {
            Tag = "muted", AutoSize = true, Font = UiConfig.Small, Margin = new Padding(0, 6, 0, 2),
            Text = s.ElectricityConfigured
                ? $"Тариф обновлён: {s.ElectricityUpdated:dd.MM.yyyy}. Напоминание придёт через полгода."
                : "Тариф ещё не задан — укажите его один раз."
        });
        chkConfirm = Check(t, "Тариф проверен и актуален", false);

        Section(t, "Зарплата оператора");
        chkSalary = Check(t, "Учитывать ЗП оператора печати", s.SalaryEnabled);
        tbSalary = Field(t, "ЗП в месяц, ₽", s.MonthlySalary);
        tbDays = Field(t, "Рабочих дней в месяце", s.WorkDays);
        tbHours = Field(t, "Часов печати принтера в сутки", s.PrinterHoursPerDay);

        Section(t, "Формула расчёта");
        tbPayback = Field(t, "Срок окупаемости и амортизации, мес.", s.PaybackMonths);
        tbRelPct = Field(t, "Сопутствующие расходы, % от цены принтера", s.RelatedPercent);
        tbRelMonths = Field(t, "Период сопутствующих расходов, мес.", s.RelatedMonths);
        tbExtra = Field(t, "Добавка к массе пластика, %", s.PlasticExtraPercent);
        tbMargin = Field(t, "Наценка на себестоимость, %", s.MarginPercent);

        Section(t, "Цена принтера P2S Combo + AMS 2 Pro");
        chkManual = Check(t, "Задать цену вручную (без автообновления)", s.UseManualPrice);
        tbManual = Field(t, "Цена вручную, ₽", s.ManualPrinterPrice);

        Caption(t, "Ссылки на страницы товара в магазинах (по одной в строке):");
        tbUrls = new TextBox
        {
            Multiline = true, Height = 100, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle,
            Font = UiConfig.Body, Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0), WordWrap = false,
            Text = string.Join(Environment.NewLine, s.PriceUrls)
        };
        t.Controls.Add(tbUrls);

        var btnCheck = new BambuButton
        {
            Text = "Проверить и обновить цену", Primary = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 10, 0, 0)
        };
        btnCheck.Click += async (_, _) => await CheckPrice(btnCheck);
        t.Controls.Add(btnCheck);

        tbLog = new TextBox
        {
            Multiline = true, ReadOnly = true, Height = 96, ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle, Font = UiConfig.Small,
            Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 8, 0, 0)
        };
        t.Controls.Add(tbLog);

        card.Controls.Add(t);

        var bottom = new BufferedTable
        {
            Dock = DockStyle.Bottom, Height = 62, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 14, 0, 0)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var cancel = new BambuButton { Text = "Отмена", Primary = false, Width = 110, Margin = new Padding(0, 0, 10, 0) };
        var ok = new BambuButton { Text = "Сохранить", Width = 140, Margin = new Padding(0) };
        ok.Click += (_, _) => SaveAndClose();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        bottom.Controls.Add(new Panel(), 0, 0);
        bottom.Controls.Add(cancel, 1, 0);
        bottom.Controls.Add(ok, 2, 0);

        Controls.Add(card);
        Controls.Add(bottom);

        Theme.Apply(this);
        HandleCreated += (_, _) => Theme.SetTitleBar(Handle);
    }

    static void Section(TableLayoutPanel t, string text)
    {
        t.Controls.Add(new Label
        {
            Text = text, Font = UiConfig.Section, AutoSize = true, Margin = new Padding(0, 18, 0, 6)
        });
    }

    static void Caption(TableLayoutPanel t, string text)
    {
        t.Controls.Add(new Label
        {
            Text = text, Tag = "muted", AutoSize = true, Margin = new Padding(0, 10, 0, 3)
        });
    }

    static CheckBox Check(TableLayoutPanel t, string text, bool value)
    {
        var c = new CheckBox { Text = text, Checked = value, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
        t.Controls.Add(c);
        return c;
    }

    static InputBox Field(TableLayoutPanel t, string caption, double value)
    {
        Caption(t, caption);
        var ib = new InputBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0),
            Text = value.ToString("0.##", CultureInfo.InvariantCulture)
        };
        t.Controls.Add(ib);
        return ib;
    }

    async Task CheckPrice(BambuButton btn)
    {
        _s.PriceUrls = ParseUrls();
        btn.Enabled = false;
        tbLog.Text = "Проверяю… (если сайт грузится скриптом, может занять до минуты)";
        try
        {
            var (ok, msg) = await PriceService.UpdateAsync(_s);
            tbLog.Text = (ok ? $"Цена обновлена: {Fmt.Rub0(_s.PrinterPrice)}" : "Цену получить не удалось")
                         + Environment.NewLine + msg.Replace("\n", Environment.NewLine);
        }
        finally
        {
            btn.Enabled = true;
        }
    }

    List<string> ParseUrls() => tbUrls.Lines
        .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    void SaveAndClose()
    {
        if (!Valid(tbTariff, "Тариф", 0.01) || !Valid(tbPower, "Мощность", 1) ||
            !Valid(tbDays, "Рабочие дни", 1) || !Valid(tbHours, "Часы печати в сутки", 1) ||
            !Valid(tbPayback, "Срок окупаемости", 1) || !Valid(tbRelMonths, "Период сопутствующих", 1) ||
            !Valid(tbRelPct, "Сопутствующие, %", 0) || !Valid(tbExtra, "Добавка к массе", 0) ||
            !Valid(tbMargin, "Наценка", 0) || !Valid(tbSalary, "ЗП", 0) || !Valid(tbManual, "Цена вручную", 0))
            return;

        double newTariff = tbTariff.Number;
        bool tariffChanged = Math.Abs(newTariff - _s.ElectricityTariff) > 0.0001;
        if (!_s.ElectricityConfigured || tariffChanged || chkConfirm.Checked)
            _s.ElectricityUpdated = DateTime.Now;
        _s.ElectricityConfigured = true;
        _s.ElectricityTariff = newTariff;
        _s.PrinterPowerW = tbPower.Number;

        _s.DarkTheme = chkDark.Checked;
        _s.SalaryEnabled = chkSalary.Checked;
        _s.MonthlySalary = tbSalary.Number;
        _s.WorkDays = tbDays.Number;
        _s.PrinterHoursPerDay = tbHours.Number;

        _s.PaybackMonths = tbPayback.Number;
        _s.RelatedPercent = tbRelPct.Number;
        _s.RelatedMonths = tbRelMonths.Number;
        _s.PlasticExtraPercent = tbExtra.Number;
        _s.MarginPercent = tbMargin.Number;

        _s.UseManualPrice = chkManual.Checked;
        _s.ManualPrinterPrice = tbManual.Number;
        _s.PriceUrls = ParseUrls();

        _s.Save();
        DialogResult = DialogResult.OK;
    }

    bool Valid(InputBox box, string name, double min)
    {
        if (!Fmt.TryNum(box.Text, out var v) || v < min)
        {
            MessageBox.Show(this, $"Поле «{name}»: введите число не меньше {min}.", "Проверьте данные",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            box.Tb.Focus();
            return false;
        }
        return true;
    }
}
