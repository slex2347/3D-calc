using System.ComponentModel;

namespace PrintCalc3D;

/// <summary>Окно «База пластика»: таблица материалов и цен за кг.</summary>
public class FilamentForm : Form
{
    readonly AppSettings _s;
    readonly BindingList<Filament> _list;
    readonly DataGridView _grid = new();

    public FilamentForm(AppSettings s)
    {
        _s = s;
        _list = new BindingList<Filament>(s.Filaments
            .Select(f => new Filament { Name = f.Name, PricePerKg = f.PricePerKg, UpdatedAt = f.UpdatedAt }).ToList());

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "База пластика";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(680, 580);
        MinimumSize = new Size(560, 440);
        Font = UiConfig.Body;
        Padding = new Padding(14);
        ShowInTaskbar = false;

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(16) };

        var hint = new Label
        {
            Text = "Цены обновляйте вручную. Новый материал — в пустой нижней строке, удаление — клавиша Delete.",
            Tag = "muted", Dock = DockStyle.Top, Height = 50
        };

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowTemplate.Height = 32;
        _grid.ColumnHeadersHeight = 36;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Материал", DataPropertyName = nameof(Filament.Name), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "₽ / кг", DataPropertyName = nameof(Filament.PricePerKg), Width = 110,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "0.##" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Обновлено", DataPropertyName = nameof(Filament.UpdatedAt), Width = 130, ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" }
        });
        _grid.DataSource = _list;
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1 && e.RowIndex < _list.Count)
            {
                _list[e.RowIndex].UpdatedAt = DateTime.Now;
                _grid.InvalidateRow(e.RowIndex);
            }
        };

        var bottom = new BufferedTable
        {
            Dock = DockStyle.Bottom, Height = 60, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 12, 0, 0)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var cancel = new BambuButton { Text = "Отмена", Primary = false, Width = 110, Margin = new Padding(0, 0, 10, 0) };
        var ok = new BambuButton { Text = "Сохранить", Width = 140, Margin = new Padding(0) };
        ok.Click += (_, _) =>
        {
            _grid.EndEdit();
            _s.Filaments = _list.Where(f => !string.IsNullOrWhiteSpace(f.Name)).ToList();
            DialogResult = DialogResult.OK;
        };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        bottom.Controls.Add(new Panel(), 0, 0);
        bottom.Controls.Add(cancel, 1, 0);
        bottom.Controls.Add(ok, 2, 0);

        card.Controls.Add(_grid);
        card.Controls.Add(hint);
        card.Controls.Add(bottom);
        Controls.Add(card);

        Theme.Apply(this);
        HandleCreated += (_, _) => Theme.SetTitleBar(Handle);
    }
}
