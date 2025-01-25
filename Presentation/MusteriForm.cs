using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OtelRezervasyon.Models;      // Musteri için
using OtelRezervasyon.DataAccess;  // MusteriDAL için

namespace OtelRezervasyon.Presentation
{
    public partial class MusteriForm : Form
    {
        private readonly MusteriDAL _musteriDAL = new MusteriDAL();
        private readonly Musteri _musteri;
        private bool _isUpdate;

        private TextBox txtTcKimlik, txtAd, txtSoyad, txtEmail, txtTelefon, txtAdres;
        private Button btnKaydet, btnIptal;

        public MusteriForm()
        {
            InitializeComponent();
            _musteri = new Musteri();
            _isUpdate = false;
            SetupForm();
        }

        public MusteriForm(Musteri musteri)
        {
            InitializeComponent();
            _musteri = musteri;
            _isUpdate = true;
            SetupForm();
            FillForm();
        }

        private void SetupForm()
        {
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Renk tanımlamaları
            var errorColor = Color.MistyRose;
            var validColor = Color.FromArgb(220, 255, 220); // Soft yeşil
            var defaultColor = SystemColors.Window;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 7,
                ColumnCount = 2
            };

            // TextBox'lar
            txtTcKimlik = new TextBox();
            txtAd = new TextBox();
            txtSoyad = new TextBox();
            txtEmail = new TextBox();
            txtTelefon = new TextBox();
            txtAdres = new TextBox();

            // Butonlar
            btnKaydet = new Button { Text = "Kaydet" };
            btnIptal = new Button { Text = "İptal" };

            // Event handlers
            btnKaydet.Click += BtnKaydet_Click;
            btnIptal.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            // Layout
            panel.Controls.Add(new Label { Text = "TC Kimlik:" }, 0, 0);
            panel.Controls.Add(txtTcKimlik, 1, 0);
            panel.Controls.Add(new Label { Text = "Ad:" }, 0, 1);
            panel.Controls.Add(txtAd, 1, 1);
            panel.Controls.Add(new Label { Text = "Soyad:" }, 0, 2);
            panel.Controls.Add(txtSoyad, 1, 2);
            panel.Controls.Add(new Label { Text = "Email:" }, 0, 3);
            panel.Controls.Add(txtEmail, 1, 3);
            panel.Controls.Add(new Label { Text = "Telefon:" }, 0, 4);
            panel.Controls.Add(txtTelefon, 1, 4);
            panel.Controls.Add(new Label { Text = "Adres:" }, 0, 5);
            panel.Controls.Add(txtAdres, 1, 5);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                Height = 40
            };
            buttonPanel.Controls.AddRange(new Control[] { btnIptal, btnKaydet });

            this.Controls.AddRange(new Control[] { panel, buttonPanel });

            // Telefon validasyonu - sadece uzunluk kontrolü
            txtTelefon.TextChanged += (s, e) => {
                var telefonText = txtTelefon.Text.Replace("-", "").Replace("_", "");
                bool isValid = telefonText.Length == 11;  // Sadece 11 hane kontrolü
                txtTelefon.BackColor = isValid ? validColor : errorColor;
                ValidateForm();
            };
        }

        private void FillForm()
        {
            txtTcKimlik.Text = _musteri.TcKimlik;
            txtAd.Text = _musteri.Ad;
            txtSoyad.Text = _musteri.Soyad;
            txtEmail.Text = _musteri.Email;
            txtTelefon.Text = _musteri.Telefon;
            txtAdres.Text = _musteri.Adres;
        }

        private void BtnKaydet_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateForm()) return;

                var musteri = new Musteri
                {
                    TcKimlik = txtTcKimlik.Text,
                    Ad = txtAd.Text,
                    Soyad = txtSoyad.Text,
                    Telefon = txtTelefon.Text,
                    Email = txtEmail.Text,
                    Adres = txtAdres.Text
                };

                if (_isUpdate)
                {
                    musteri.MusteriId = _musteri.MusteriId;
                    if (_musteriDAL.UpdateMusteri(musteri))
                    {
                        MessageBox.Show("Müşteri başarıyla güncellendi.", "Başarılı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                }
                else
                {
                    int musteriId = _musteriDAL.AddMusteri(musteri);
                    if (musteriId > 0)
                    {
                        MessageBox.Show("Müşteri başarıyla eklendi.", "Başarılı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Müşteri eklenirken bir hata oluştu.", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateForm()
        {
            bool isValid = true;

            // Telefon kontrolü - sadece uzunluk
            var telefonText = txtTelefon.Text.Replace("-", "").Replace("_", "");
            if (telefonText.Length != 11)  // Sadece 11 hane kontrolü
            {
                isValid = false;
            }

            return isValid;
        }
    }
}
