using System.Globalization;

namespace PrintCalc3D;

/// <summary>
/// Левая карточка «Параметры заказа»: материал, цена за кг, время, масса, кнопка «Рассчитать»,
/// ЗП, заказы в месяц. Любое изменение поля даёт событие Changed (результат при этом НЕ пересчитывается),
/// сам расчёт запускается кнопкой «Рассчитать» (CalculateClicked) или клавишей Enter.
/// </summary>
public class InputsPanel : Card
{
    readonly AppSettings S;
    ComboBox _cbFil;
    InputBox _price, _h, _m, _gr, _orders;
    CheckBox _chkSalary;
    Label _massHint, _calcHint;
    BambuButton _btnCalc;
    bool _loading = true;

    public event EventHandler Changed;
    public event EventHandler CalculateClicked;

    public double Hours => _h.Number + _m.Number / 60.0;
    public double Grams => _gr.Number;
    public double PricePerKg => _price.Number;
    public double Orders => _orders.Number;

    /// <summary>Кнопка «Рассчитать» — главное окно назначает её кнопкой по умолчанию (Enter).</summary>
    public BambuButton CalculateButton => _btnCalc;

    public InputsPanel(AppSettings settings)
    {
        S = settings;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Build();
        _loading = false;

        // подсказки переносятся по ширине карточки
        SizeChanged += (_, _) =>
        {
            int w = Math.Max(120, ClientSize.Width - 60);
            _massHint.MaximumSize = new Size(w, 0);
            _calcHint.MaximumSize = new Size(w, 0);
        };
    }

    void Raise()
    {
        if (_loading) return;
        SetHint(UiText.HintStale, true);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ------------------------------------------------------------------ Публичные методы

    public void UpdateMassHint(double extraPercent)
    {
        double g = Grams;
        string text = $"К массе из слайсера добавляется {extraPercent:0.#}%" +
                      (g > 0 ? $"  →  {g * (1 + extraPercent / 100.0):0.#} г" : "");
        if (_massHint.Text != text) _massHint.Text = text;
    }

    /// <summary>Расчёт выполнен — подсказка под кнопкой «Рассчитать».</summary>
    public void MarkFresh() => SetHint(UiText.HintFresh, false);

    /// <summary>Подтянуть состояние галочки ЗП из настроек (после закрытия окна настроек).</summary>
    public void SyncSalary()
    {
        _loading = true;
        _chkSalary.Checked = S.SalaryEnabled;
        _loading = false;
    }

    public void ReloadFilaments() => FillFilamentCombo();

    void SetHint(string text, bool accent)
    {
        if (_calcHint.Text != text) _calcHint.Text = text;
        _calcHint.Tag = accent ? "accent" : "muted";
        _calcHint.ForeColor = accent ? Theme.Accent : Theme.Muted;
    }

    // ------------------------------------------------------------------ Построение

    void Build()
    {
        var t = new BufferedTable
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = UiConfig.CardPadding
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        t.Controls.Add(new Label
        {
            Text = UiText.InputsTitle, Font = UiConfig.CardTitle, AutoSize = true, Margin = new Padding(0, 0, 0, 4)
        });

        // Материал
        AddCaption(t, UiText.LblMaterial);
        _cbFil = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiConfig.Input };
        FillFilamentCombo();
        _cbFil.SelectedIndexChanged += (_, _) => OnFilamentSelected();
        AddControl(t, _cbFil);

        AddCaption(t, UiText.LblPricePerKg);
        _price = new InputBox();
        _price.ValueChanged += (_, _) => Raise();
        AddControl(t, _price);

        var btns = new BufferedTable { ColumnCount = 2, RowCount = 1, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        btns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var btnSave = new BambuButton { Text = UiText.BtnSaveToDb, Primary = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 4, 0) };
        var btnDb = new BambuButton { Text = UiText.BtnFilamentDb, Primary = false, Dock = DockStyle.Fill, Margin = new Padding(4, 0, 0, 0) };
        btnSave.Click += (_, _) => SavePriceToDb();
        btnDb.Click += (_, _) => OpenFilamentDb();
        btns.Controls.Add(btnSave, 0, 0);
        btns.Controls.Add(btnDb, 1, 0);
        AddControl(t, btns);

        // Время
        AddCaption(t, UiText.LblTime, new Padding(0, 18, 0, 4));
        var time = new BufferedTable { ColumnCount = 4, RowCount = 1, AutoSize = true };
        time.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        time.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        time.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        time.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _h = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0) };
        _m = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0) };
        _h.ValueChanged += (_, _) => Raise();
        _m.ValueChanged += (_, _) => Raise();
        time.Controls.Add(_h, 0, 0);
        time.Controls.Add(new Label { Text = UiText.UnitHours, Tag = "muted", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 14, 0) }, 1, 0);
        time.Controls.Add(_m, 2, 0);
        time.Controls.Add(new Label { Text = UiText.UnitMinutes, Tag = "muted", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 0, 0) }, 3, 0);
        AddControl(t, time);

        // Масса
        AddCaption(t, UiText.LblGrams);
        _gr = new InputBox();
        _gr.ValueChanged += (_, _) => Raise();
        AddControl(t, _gr);
        _massHint = new Label
        {
            Tag = "muted", AutoSize = true, Font = UiConfig.Small, Margin = new Padding(0, 4, 0, 0)
        };
        t.Controls.Add(_massHint);

        // Кнопка «Рассчитать» (сразу под временем и массой) и подсказка о состоянии расчёта
        _btnCalc = new BambuButton { Text = UiText.BtnCalculate, Primary = true, Height = 46 };
        _btnCalc.Click += (_, _) => CalculateClicked?.Invoke(this, EventArgs.Empty);
        AddControl(t, _btnCalc);
        _btnCalc.Margin = new Padding(0, 16, 0, 0);

        _calcHint = new Label
        {
            Tag = "muted", AutoSize = true, Font = UiConfig.Small, Margin = new Padding(0, 6, 0, 0),
            Text = UiText.HintInitial
        };
        t.Controls.Add(_calcHint);

        // ЗП оператора
        _chkSalary = new CheckBox
        {
            Text = UiText.ChkSalary, AutoSize = true, Checked = S.SalaryEnabled, Margin = new Padding(0, 20, 0, 0)
        };
        _chkSalary.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            S.SalaryEnabled = _chkSalary.Checked;
            S.Save();
            Raise();
        };
        t.Controls.Add(_chkSalary);

        // Заказы в месяц
        AddCaption(t, UiText.LblOrders, new Padding(0, 18, 0, 4));
        _orders = new InputBox();
        _orders.ValueChanged += (_, _) => Raise();
        AddControl(t, _orders);

        Controls.Add(t);
    }

    static void AddCaption(TableLayoutPanel t, string text, Padding? margin = null)
    {
        t.Controls.Add(new Label
        {
            Text = text, Tag = "muted", AutoSize = true, Margin = margin ?? new Padding(0, 12, 0, 4)
        });
    }

    static void AddControl(TableLayoutPanel t, Control c)
    {
        c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        c.Margin = new Padding(0);
        t.Controls.Add(c);
    }

    // ------------------------------------------------------------------ База пластика

    void FillFilamentCombo()
    {
        _cbFil.Items.Clear();
        _cbFil.Items.Add(UiText.CustomFilament);
        foreach (var f in S.Filaments) _cbFil.Items.Add(f.Name);
        _cbFil.SelectedIndex = 0;
    }

    void OnFilamentSelected()
    {
        if (_cbFil.SelectedIndex <= 0) return;
        var f = S.Filaments[_cbFil.SelectedIndex - 1];
        _price.Text = f.PricePerKg.ToString("0.##", CultureInfo.InvariantCulture);
    }

    void SavePriceToDb()
    {
        var owner = FindForm();
        double price = _price.Number;
        if (price <= 0)
        {
            MessageBox.Show(owner, "Введите цену пластика за кг.", "Цена", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_cbFil.SelectedIndex > 0)
        {
            var f = S.Filaments[_cbFil.SelectedIndex - 1];
            f.PricePerKg = price;
            f.UpdatedAt = DateTime.Now;
        }
        else
        {
            string name = Prompt.Ask(owner, "Новый материал", "Название материала для базы:", "");
            if (string.IsNullOrWhiteSpace(name)) return;
            S.Filaments.Add(new Filament { Name = name.Trim(), PricePerKg = price, UpdatedAt = DateTime.Now });
            FillFilamentCombo();
            _cbFil.SelectedIndex = _cbFil.Items.Count - 1;
            _price.Text = price.ToString("0.##", CultureInfo.InvariantCulture);
        }
        S.Save();
    }

    void OpenFilamentDb()
    {
        using var f = new FilamentForm(S);
        if (f.ShowDialog(FindForm()) == DialogResult.OK)
        {
            S.Save();
            FillFilamentCombo();
        }
    }
}
