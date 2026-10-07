namespace PrintCalc3D;

/// <summary>Маленькое окно с одним полем ввода (название нового материала и т.п.).</summary>
public static class Prompt
{
    public static string Ask(IWin32Window owner, string title, string caption, string initial)
    {
        using var f = new Form
        {
            AutoScaleDimensions = new SizeF(96F, 96F),
            AutoScaleMode = AutoScaleMode.Dpi,
            Text = title, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, ClientSize = new Size(440, 170), Font = UiConfig.Body,
            Padding = new Padding(14), ShowInTaskbar = false
        };
        var card = new Card { Dock = DockStyle.Fill };
        var t = new BufferedTable
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(18)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lbl = new Label { Text = caption, Tag = "muted", AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
        var ib = new InputBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0), Text = initial };
        var ok = new BambuButton { Text = "OK", Width = 110, Anchor = AnchorStyles.Right, Margin = new Padding(0, 14, 0, 0) };
        ok.Click += (_, _) => { f.DialogResult = DialogResult.OK; f.Close(); };

        t.Controls.Add(lbl);
        t.Controls.Add(ib);
        t.Controls.Add(ok);
        card.Controls.Add(t);
        f.Controls.Add(card);
        f.AcceptButton = ok;
        Theme.Apply(f);
        f.HandleCreated += (_, _) => Theme.SetTitleBar(f.Handle);
        return f.ShowDialog(owner) == DialogResult.OK ? ib.Text : null;
    }
}
