using System;
using System.Windows.Forms;
namespace PrintCalc3D {
    public partial class InputsPanel : UserControl {
        private TextBox txtWeight;
        private TextBox txtHours;
        private TextBox txtPartsCount;
        private TextBox txtBedsCount;
        private Label lblParts;
        private Label lblBeds;
        public InputsPanel() {
            InitializeComponent();
            CreateDynamicInputs();
            SetupStrictValidation();
        }
        private void CreateDynamicInputs() {
            lblParts = new Label { Text = "Количество деталей:", Top = 110, Left = 15, AutoSize = true };
            txtPartsCount = new TextBox { Text = "1", Top = 107, Left = 180, Width = 80 };
            lblBeds = new Label { Text = "Количество столов:", Top = 145, Left = 15, AutoSize = true };
            txtBedsCount = new TextBox { Text = "1", Top = 142, Left = 180, Width = 80 };
            this.Controls.Add(lblParts); this.Controls.Add(txtPartsCount);
            this.Controls.Add(lblBeds); this.Controls.Add(txtBedsCount);
        }
        private void SetupStrictValidation() {
            this.txtWeight.KeyPress += OnlyDecimal_KeyPress;
            this.txtHours.KeyPress += OnlyDecimal_KeyPress;
            this.txtPartsCount.KeyPress += OnlyInteger_KeyPress;
            this.txtBedsCount.KeyPress += OnlyInteger_KeyPress;
        }
        private void OnlyDecimal_KeyPress(object sender, KeyPressEventArgs e) {
            TextBox box = sender as TextBox;
            char sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != sep) { e.Handled = true; }
            if (e.KeyChar == sep && box.Text.Contains(sep.ToString())) { e.Handled = true; }
        }
        private void OnlyInteger_KeyPress(object sender, KeyPressEventArgs e) {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) { e.Handled = true; }
        }
        public OrderInputs GetValues() {
            return new OrderInputs {
                WeightGrams = double.TryParse(txtWeight.Text, out double w) ? w : 0,
                Hours = double.TryParse(txtHours.Text, out double h) ? h : 0,
                PartsCount = int.TryParse(txtPartsCount.Text, out int p) ? Math.Max(1, p) : 1,
                BedsCount = int.TryParse(txtBedsCount.Text, out int b) ? Math.Max(1, b) : 1
            };
        }
    }
}
