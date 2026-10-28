
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace OwolabiVentures.POS
{
    public partial class MainPOS : Form
    {
        HttpClient client = new HttpClient() { BaseAddress = new Uri("https://localhost:39025/") };
        TextBox txtQR, txtAmount, txtItem;
        ComboBox cmbShop;
        Label lblBalance, lblStatus;
        Button btnDeduct, btnCheck;

        public MainPOS()
        {
            Text = "Owolabi University - Ventures POS";
            Width = 500; Height = 600;
            BackColor = Color.White;

            var title = new Label() { Text = "OWOLABI UNIVERSITY Venture POS - Food Wallet", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = false, Width = 460, Height = 60, Top = 10, Left = 20, TextAlign = ContentAlignment.MiddleCenter };
            Controls.Add(title);

            var lblQR = new Label() { Text = "Scan Student QR:", Top = 80, Left = 20, Width = 200 };
            txtQR = new TextBox() { Top = 100, Left = 20, Width = 440, Height = 40, Font = new Font("Consolas", 12) };
            txtQR.PlaceholderText = "Scan QR here (e.g. OU-WAL-0001|ABC123)";
            Controls.Add(lblQR); Controls.Add(txtQR);

            var lblItem = new Label() { Text = "Food Item:", Top = 150, Left = 20 };
            txtItem = new TextBox() { Top = 170, Left = 20, Width = 440, Text = "Jollof Rice + Chicken" };
            Controls.Add(lblItem); Controls.Add(txtItem);

            var lblAmt = new Label() { Text = "Amount (₦):", Top = 210, Left = 20 };
            txtAmount = new TextBox() { Top = 230, Left = 20, Width = 440, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            txtAmount.Text = "1500";
            Controls.Add(lblAmt); Controls.Add(txtAmount);

            var lblShop = new Label() { Text = "Shop:", Top = 270, Left = 20 };
            cmbShop = new ComboBox() { Top = 290, Left = 20, Width = 440 };
            cmbShop.Items.AddRange(new string[] { "Main Cafeteria", "Mini Mart", "Bookshop", "Busness Center/Cafe", "Bakery" });
            cmbShop.SelectedIndex = 0;
            Controls.Add(lblShop); Controls.Add(cmbShop);

            btnDeduct = new Button() { Text = "DEDUCT MONEY", Top = 330, Left = 20, Width = 440, Height = 50, BackColor = Color.FromArgb(0, 100, 0), ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            btnDeduct.Click += BtnDeduct_Click;
            Controls.Add(btnDeduct);

            btnCheck = new Button() { Text = "Check Balance", Top = 390, Left = 20, Width = 210, Height = 40 };
            btnCheck.Click += BtnCheck_Click;
            Controls.Add(btnCheck);

            var btnClear = new Button() { Text = "Clear", Top = 390, Left = 250, Width = 210, Height = 40 };
            btnClear.Click += (s, e) => { txtQR.Clear(); txtAmount.Text = ""; lblStatus.Text = ""; };
            Controls.Add(btnClear);

            lblBalance = new Label() { Top = 440, Left = 20, Width = 440, Height = 40, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.DarkGreen };
            Controls.Add(lblBalance);

            lblStatus = new Label() { Top = 480, Left = 20, Width = 440, Height = 80, Font = new Font("Consolas", 9) };
            Controls.Add(lblStatus);
        }

        private async void BtnDeduct_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtQR.Text)) { MessageBox.Show("Scan QR first"); return; }
            var parts = txtQR.Text.Split('|');
            if (parts.Length != 2) { MessageBox.Show("Invalid QR - must be WALLETID|TOKEN"); return; }
            var payload = new { WalletId = parts[0], QRToken = parts[1], Amount = decimal.Parse(txtAmount.Text), Item = txtItem.Text, VendorShop = cmbShop.Text };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var resp = await client.PostAsync("api/wallet/deduct", content);
                var result = await resp.Content.ReadAsStringAsync();
                lblStatus.Text = result;
                if (resp.IsSuccessStatusCode) { lblBalance.Text = "SUCCESS - " + result; MessageBox.Show("Payment Successful!", "Owolabi Ventures"); txtQR.Clear(); }
                else { MessageBox.Show("FAILED: " + result); }
            }
            catch (Exception ex) { MessageBox.Show("Cannot connect to API. Is API running?\n" + ex.Message); }
        }

        private void InitializeComponent()
        {

        }

        private async void BtnCheck_Click(object? sender, EventArgs e)
        {
            try
            {
                var walletId = txtQR.Text.Split('|')[0];
                var resp = await client.GetAsync($"api/wallet/balance/{walletId}");
                var result = await resp.Content.ReadAsStringAsync();
                lblBalance.Text = result;
                lblStatus.Text = result;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
