using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
namespace PrintCalc3D {
    public partial class HeaderPanel : UserControl {
        private ComboBox cbPrinterSelect;
        private Label lblHeaderTitle;
        private string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "printers_links.json");
        public event Action<string> PrinterChanged;
        public HeaderPanel() {
            InitializeComponent();
            InitializePrinterControls();
        }
        private void InitializePrinterControls() {
            lblHeaderTitle = new Label { Text = "Модель 3D-принтера:", Top = 15, Left = 15, AutoSize = true };
            this.Controls.Add(lblHeaderTitle);
            cbPrinterSelect = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280, Top = 12, Left = 160 };
            string[] popularPrinters = {
                "Bambu Lab P2S Combo", "Bambu Lab X1-Carbon", "Bambu Lab P1S Combo", "Bambu Lab A1 Combo", "Bambu Lab A1 Mini",
                "Creality K1 Max", "Creality K1C", "Creality K2 Plus Combo", "Creality Ender-3 V3",
                "Anycubic Kobra 3 Combo", "Elegoo Neptune 4 Pro", "Elegoo Neptune 4 Max", "PICASO 3D Designer X Pro"
            };
            cbPrinterSelect.Items.AddRange(popularPrinters);
            cbPrinterSelect.SelectedIndex = 0;
            cbPrinterSelect.SelectedIndexChanged += (s, e) => { PrinterChanged?.Invoke(cbPrinterSelect.SelectedItem.ToString()); };
            this.Controls.Add(cbPrinterSelect);
            if (!File.Exists(configPath)) { CreateDefaultLinksFile(popularPrinters); }
        }
        private void CreateDefaultLinksFile(string[] printers) {
            List<string> lines = new List<string> { "{" };
            for (int i = 0; i < printers.Length; i++) {
                string comma = (i == printers.Length - 1) ? "" : ",";
                lines.Add($"  \"{printers[i]}\": [\"https://example.com\"]{comma}");
            }
            lines.Add("}");
            File.WriteAllLines(configPath, lines.ToArray());
        }
    }
}
