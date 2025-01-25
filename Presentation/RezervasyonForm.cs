using System;
using System.Windows.Forms;
using System.Drawing;
using OtelRezervasyon.DataAccess;
using OtelRezervasyon.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using OtelRezervasyon.Extensions;
using System.Transactions;

namespace OtelRezervasyon.Presentation
{
    public partial class RezervasyonForm : Form
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();
        private readonly RezervasyonDAL _rezervasyonDAL;
        private readonly OdaDAL _odaDAL;
        private readonly MusteriDAL _musteriDAL;
        private readonly FaturaDAL _faturaDAL;
        private readonly int? _rezervasyonId;

        // Form kontrolleri
        private TextBox txtTC, txtAd, txtSoyad;
        private MaskedTextBox txtTelefon;
        private ComboBox cmbOda;
        private DateTimePicker dtpGiris, dtpCikis;
        private Label lblToplamTutar;
        private Button btnIptal, btnPdfExport;

        public RezervasyonForm(int? rezervasyonId = null)
        {
            InitializeComponent();
            
            _rezervasyonId = rezervasyonId;
            _rezervasyonDAL = new RezervasyonDAL();
            _odaDAL = new OdaDAL();
            _musteriDAL = new MusteriDAL();
            _faturaDAL = new FaturaDAL();

            SetupFormControls();
            
            if (rezervasyonId.HasValue)
            {
                LoadRezervasyon(rezervasyonId.Value);
                Text = $"Rezervasyon Detay - #{rezervasyonId}";
                
                // Detay modunda Rezerve Et butonunu gizle
                btnPdfExport.Visible = false;
            }
            else
            {
                Text = "Yeni Rezervasyon";
                LoadOdalar();
            }
        }

        private void SetupFormControls()
        {
            // Form ayarları
            this.Size = new Size(550, 750);  // Form boyutunu büyüttük
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Rezervasyon";
            this.Font = new Font("Segoe UI", 10F);

            // Kontrolleri oluştur
            var y = 20;
            var labelX = 20;
            var controlX = 150;
            var controlWidth = 350;  // Kontrol genişliğini artırdık
            var spacing = 40;

            // TC Kimlik
            var lblTC = new Label { Text = "TC Kimlik:", Location = new Point(labelX, y), AutoSize = true };
            txtTC = new TextBox { 
                Location = new Point(controlX, y), 
                Width = controlWidth, 
                MaxLength = 11 
            };
            txtTC.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };
            y += spacing;

            // Ad
            var lblAd = new Label { Text = "Ad:", Location = new Point(labelX, y), AutoSize = true };
            txtAd = new TextBox { 
                Location = new Point(controlX, y), 
                Width = controlWidth,
                CharacterCasing = CharacterCasing.Upper  // Otomatik büyük harf
            };
            y += spacing;

            // Soyad
            var lblSoyad = new Label { Text = "Soyad:", Location = new Point(labelX, y), AutoSize = true };
            txtSoyad = new TextBox { 
                Location = new Point(controlX, y), 
                Width = controlWidth,
                CharacterCasing = CharacterCasing.Upper  // Otomatik büyük harf
            };
            y += spacing;

            // Telefon
            var lblTelefon = new Label { Text = "Telefon:", Location = new Point(labelX, y), AutoSize = true };
            txtTelefon = new MaskedTextBox { 
                Location = new Point(controlX, y), 
                Width = controlWidth,
                Mask = "0000-000-0000",  // 11 hane için maske
                PromptChar = '_',
                TextMaskFormat = MaskFormat.ExcludePromptAndLiterals,
                HidePromptOnLeave = true,
                BeepOnError = false
            };

            // Telefon için özel event handlers
            txtTelefon.Click += (s, e) => {
                if (txtTelefon.Text.Length == 0)
                {
                    txtTelefon.Select(0, 0);
                }
            };

            txtTelefon.GotFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtTelefon.Text))
                {
                    txtTelefon.Select(0, 0);
                }
            };

            txtTelefon.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Back && txtTelefon.SelectionStart <= 1)
                {
                    txtTelefon.Select(0, 0);
                }
            };
            y += spacing;

            // Oda Seçimi
            var lblOda = new Label { Text = "Oda:", Location = new Point(labelX, y), AutoSize = true };
            cmbOda = new ComboBox { 
                Location = new Point(controlX, y), 
                Width = controlWidth, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                Height = 25
            };
            
            // Oda listesi özelleştirme
            cmbOda.DrawItem += (s, e) => {
                e.DrawBackground();
                if (e.Index >= 0)
                {
                    var row = ((DataRowView)cmbOda.Items[e.Index]).Row;
                    var durum = row["durum"].ToString();
                    var odaNo = row["odaNumarasi"].ToString();
                    var odaTipi = row["odaTipi"].ToString();
                    var fiyat = Convert.ToDecimal(row["temelFiyat"]);
                    var ozellikler = row["ozellikler"]?.ToString() ?? "";

                    // Arka plan rengi - Daha soft renkler kullanıyoruz
                    Color bgColor;
                    if (durum == "BOŞ")
                        bgColor = Color.FromArgb(220, 255, 220);  // Soft yeşil
                    else if (durum == "DOLU")
                        bgColor = Color.FromArgb(255, 220, 220);  // Soft kırmızı
                    else if (durum == "TEMİZLİK")
                        bgColor = Color.FromArgb(255, 255, 200);  // Soft sarı
                    else if (durum == "BAKIM")
                        bgColor = Color.FromArgb(200, 200, 255);  // Soft mavi
                    else
                        bgColor = SystemColors.Window;

                    // Seçili öğe için daha koyu arka plan
                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    {
                        bgColor = Color.FromArgb(
                            Math.Max(bgColor.R - 30, 0),
                            Math.Max(bgColor.G - 30, 0),
                            Math.Max(bgColor.B - 30, 0)
                        );
                    }

                    using (var brush = new SolidBrush(bgColor))
                        e.Graphics.FillRectangle(brush, e.Bounds);

                    // Metin çizimi - İki satırlı gösterim
                    var normalFont = e.Font;
                    var boldFont = new Font(normalFont, FontStyle.Bold);

                    // Birinci satır (Kalın yazı ile)
                    var line1 = $"Oda {odaNo} - {odaTipi} - {fiyat:C}";
                    var line1Bounds = new Rectangle(
                        e.Bounds.X + 5,
                        e.Bounds.Y + 2,
                        e.Bounds.Width - 10,
                        20);

                    // İkinci satır (Normal yazı ile)
                    var line2 = ozellikler;
                    var line2Bounds = new Rectangle(
                        e.Bounds.X + 5,
                        e.Bounds.Y + 22,
                        e.Bounds.Width - 10,
                        18);

                    // Metinleri çiz
                    using (var brush = new SolidBrush(Color.Black))  // Her zaman siyah metin
                    {
                        e.Graphics.DrawString(line1, boldFont, brush, line1Bounds);
                        if (!string.IsNullOrEmpty(line2))
                            e.Graphics.DrawString(line2, normalFont, brush, line2Bounds);
                    }

                    // Durum göstergesi (sağ üst köşede küçük daire)
                    Color statusColor = Color.Gray; // Varsayılan renk

                    if (durum == "BOŞ")
                        statusColor = Color.Green;
                    else if (durum == "DOLU")
                        statusColor = Color.Red;
                    else if (durum == "TEMİZLİK")
                        statusColor = Color.Orange;
                    else if (durum == "BAKIM")
                        statusColor = Color.Blue;

                    using (var brush = new SolidBrush(statusColor))
                    {
                        e.Graphics.FillEllipse(brush, 
                            e.Bounds.Right - 15, 
                            e.Bounds.Top + 5, 
                            10, 10);
                    }
                }
            };
            
            // Oda listesi yüksekliği
            cmbOda.DropDownHeight = 350;
            cmbOda.ItemHeight = 40;  // Her öğe için daha fazla alan
            y += spacing + 10;

            // Tarih seçimleri
            var lblGiris = new Label { Text = "Giriş:", Location = new Point(labelX, y), AutoSize = true };
            dtpGiris = new DateTimePicker
            {
                Location = new Point(controlX, y),
                Width = controlWidth,
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                MaxDate = DateTime.Today.AddYears(1),
                Value = DateTime.Today
            };
            y += spacing;

            var lblCikis = new Label { Text = "Çıkış:", Location = new Point(labelX, y), AutoSize = true };
            dtpCikis = new DateTimePicker
            {
                Location = new Point(controlX, y),
                Width = controlWidth,
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today.AddDays(1),
                MaxDate = DateTime.Today.AddYears(1),
                Value = DateTime.Today.AddDays(1)
            };
            y += spacing;

            // Özet Label - Form genişliğine göre ayarla
            lblToplamTutar = new Label
            {
                Location = new Point(labelX, y),
                Size = new Size(this.ClientSize.Width - (2 * labelX), 200),  // Genişliği form genişliğine göre ayarla
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke,
                Font = new Font("Consolas", 10),
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top  // Yatayda otomatik genişleme
            };
            y += lblToplamTutar.Height + spacing;

            // İptal butonu
            btnIptal = new Button
            {
                Text = "İptal",
                Location = new Point(controlX, y),
                Width = controlWidth,
                Height = 40,
                DialogResult = DialogResult.Cancel
            };
            btnIptal.ApplyCustomStyle("danger");  // Kırmızı buton

            // PDF butonunu düzelt
            btnPdfExport = new Button
            {
                Text = "Rezerve Et",
                Location = new Point(controlX, y),
                Width = controlWidth,
                Height = 40
            };
            btnPdfExport.ApplyCustomStyle("primary");  // Mavi stil

            // Input validation renkleri
            var errorColor = Color.FromArgb(255, 233, 233);
            var validColor = Color.FromArgb(233, 255, 233);

            // TC Kimlik validasyonu
            txtTC.TextChanged += (s, e) => {
                txtTC.BackColor = (txtTC.Text.Length == 11 && txtTC.Text.All(char.IsDigit)) 
                    ? validColor : errorColor;
            };

            // Ad validasyonu
            txtAd.TextChanged += (s, e) => {
                txtAd.BackColor = txtAd.Text.Length >= 2 ? validColor : errorColor;
            };

            // Soyad validasyonu
            txtSoyad.TextChanged += (s, e) => {
                txtSoyad.BackColor = txtSoyad.Text.Length >= 2 ? validColor : errorColor;
            };

            // Telefon validasyonu
            txtTelefon.TextChanged += (s, e) => {
                var telefon = new string(txtTelefon.Text.Where(char.IsDigit).ToArray());
                bool isValid = telefon.Length == 11 && telefon.StartsWith("05");  // 11 hane kontrolü
                txtTelefon.BackColor = isValid ? validColor : errorColor;
            };

            // Event handlers
            cmbOda.SelectedIndexChanged += (s, e) => HesaplaFiyat();
            dtpGiris.ValueChanged += (s, e) => {
                if (dtpCikis.Value <= dtpGiris.Value)
                {
                    dtpCikis.Value = dtpGiris.Value.AddDays(1);
                }
                HesaplaFiyat();
            };
            dtpCikis.ValueChanged += (s, e) => {
                if (dtpCikis.Value <= dtpGiris.Value)
                {
                    MessageBox.Show(
                        "Çıkış tarihi giriş tarihinden sonra olmalıdır.",
                        "Uyarı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    dtpCikis.Value = dtpGiris.Value.AddDays(1);
                }
                HesaplaFiyat();
            };

            // Kontrolleri forma ekle
            this.Controls.AddRange(new Control[] {
                lblTC, txtTC,
                lblAd, txtAd,
                lblSoyad, txtSoyad,
                lblTelefon, txtTelefon,
                lblOda, cmbOda,
                lblGiris, dtpGiris,
                lblCikis, dtpCikis,
                lblToplamTutar,
                btnPdfExport,
                btnIptal
            });

            // Form boyutunu ayarla
            this.ClientSize = new Size(
                Math.Max(labelX + controlWidth + labelX, lblToplamTutar.Right + labelX),
                btnPdfExport.Bottom + labelX
            );

            // Form'un iptal butonunu ayarla
            this.CancelButton = btnIptal;

            btnPdfExport.Click += (s, e) => {
                try
                {
                    if (!ValidateForm()) return;

                    // Müşteri bilgilerini hazırla
                    var musteri = new Musteri
                    {
                        TcKimlik = txtTC.Text,
                        Ad = txtAd.Text.Trim(),
                        Soyad = txtSoyad.Text.Trim(),
                        Telefon = txtTelefon.Text
                    };

                    // Rezervasyon bilgilerini hazırla
                    var selectedRow = ((DataRowView)cmbOda.SelectedItem).Row;
                    var odaId = Convert.ToInt32(selectedRow["odaId"]);
                    var gunlukFiyat = Convert.ToDecimal(selectedRow["temelFiyat"]);
                    var gunSayisi = (dtpCikis.Value - dtpGiris.Value).Days;
                    var toplamTutar = gunlukFiyat * gunSayisi;

                    var rezervasyon = new Rezervasyon
                    {
                        OdaId = odaId,
                        GirisTarihi = dtpGiris.Value,
                        CikisTarihi = dtpCikis.Value,
                        Fiyat = toplamTutar,
                        Durum = "AKTIF"
                    };

                    // Rezervasyonu oluştur
                    int rezervasyonId = _rezervasyonDAL.CreateRezervasyonWithFatura(musteri, rezervasyon);
                    if (rezervasyonId == -1) return;

                    // Odayı DOLU olarak işaretle
                    _odaDAL.UpdateOdaDurum(odaId, "DOLU");

                    // Fatura oluştur
                    var fatura = new Fatura
                    {
                        rezervasyonId = rezervasyonId,
                        tutar = toplamTutar,
                        durum = "ODENMEDI",
                        tarih = DateTime.Now
                    };

                    _faturaDAL.AddFatura(fatura);

                    MessageBox.Show(
                        "Rezervasyon başarıyla oluşturuldu!", 
                        "Başarılı", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}");
                }
            };
        }

        private void LoadRezervasyon(int rezervasyonId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            r.*,
                            m.tcKimlik,
                            m.ad,
                            m.soyad,
                            m.telefon,
                            o.odaNumarasi,
                            o.temelFiyat
                        FROM rezervasyon r
                        JOIN musteri m ON r.musteriId = m.musteriId
                        JOIN oda o ON r.odaId = o.odaId
                        WHERE r.rezervasyonId = @id", conn);

                    cmd.Parameters.AddWithValue("@id", rezervasyonId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Müşteri bilgileri
                            txtTC.Text = reader["tcKimlik"].ToString();
                            txtAd.Text = reader["ad"].ToString();
                            txtSoyad.Text = reader["soyad"].ToString();
                            txtTelefon.Text = reader["telefon"].ToString();

                            // Tarihleri güvenli bir şekilde ayarla
                            var giris = Convert.ToDateTime(reader["girisTarihi"]);
                            var cikis = Convert.ToDateTime(reader["cikisTarihi"]);

                            dtpGiris.MinDate = DateTime.Today.AddYears(-1);
                            dtpGiris.MaxDate = DateTime.Today.AddYears(1);
                            dtpCikis.MinDate = DateTime.Today.AddYears(-1);
                            dtpCikis.MaxDate = DateTime.Today.AddYears(1);

                            dtpGiris.Value = giris;
                            dtpCikis.Value = cikis;

                            // Oda bilgisi
                            LoadOdalar();  // Önce odaları yükle
                            var odaNo = reader["odaNumarasi"].ToString();
                            foreach (DataRowView item in cmbOda.Items)
                            {
                                if (item["odaNumarasi"].ToString() == odaNo)
                                {
                                    cmbOda.SelectedItem = item;
                                    break;
                                }
                            }

                            // Kontrolleri devre dışı bırak
                            txtTC.ReadOnly = true;
                            txtAd.ReadOnly = true;
                            txtSoyad.ReadOnly = true;
                            txtTelefon.ReadOnly = true;
                            dtpGiris.Enabled = false;
                            dtpCikis.Enabled = false;
                            cmbOda.Enabled = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon bilgileri yüklenirken hata oluştu: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void LoadOdalar()
        {
            try
            {
                var odalar = _odaDAL.GetBosOdalar();
                if (odalar != null && odalar.Columns.Contains("odaId"))
                {
                    cmbOda.DataSource = odalar;
                    cmbOda.DisplayMember = "odaNumarasi";
                    cmbOda.ValueMember = "odaId";
                }
                else
                {
                    MessageBox.Show("Oda listesi yüklenemedi!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Oda listesi yüklenirken hata: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HesaplaFiyat()
        {
            if (cmbOda.SelectedItem == null) return;

            var selectedRow = ((DataRowView)cmbOda.SelectedItem).Row;
            var gunlukFiyat = Convert.ToDecimal(selectedRow["temelFiyat"]);
            
            // Gün farkını hesapla (son gün dahil)
            var gunSayisi = (dtpCikis.Value.Date - dtpGiris.Value.Date).Days;

            if (gunSayisi < 1)  // En az 1 gece konaklamaya izin ver
            {
                lblToplamTutar.Text = @"
⚠️ UYARI
────────────────────────
En az 1 gece konaklama yapılmalıdır.
Çıkış tarihi, giriş tarihinden sonra olmalıdır.

Örnek:
Giriş: 01.03.2024
Çıkış: 02.03.2024 = 1 gece konaklama";
                return;
            }

            var toplamTutar = gunlukFiyat * gunSayisi;

            lblToplamTutar.Text = $@"
REZERVASYON ÖZETİ
────────────────────────
Oda No: {selectedRow["odaNumarasi"]}
Giriş: {dtpGiris.Value:dd.MM.yyyy} (14:00)
Çıkış: {dtpCikis.Value:dd.MM.yyyy} (12:00)
Süre: {gunSayisi} gece

Gecelik: {gunlukFiyat:C2}
TOPLAM: {toplamTutar:C2}

Not: Giriş saati 14:00, çıkış saati 12:00'dir.";
        }

        private void RezervasyonForm_Load(object sender, EventArgs e)
        {
            // Input validation için renk tanımları
            Color errorColor = Color.FromArgb(255, 233, 233);
            Color validColor = Color.FromArgb(233, 255, 233);

            // TC Kimlik validasyonu
            txtTC.TextChanged += (s, ev) => {
                txtTC.BackColor = (string.IsNullOrEmpty(txtTC.Text) || txtTC.Text.Length != 11) 
                    ? errorColor : validColor;
            };

            // Ad validasyonu
            txtAd.TextChanged += (s, ev) => {
                txtAd.BackColor = string.IsNullOrWhiteSpace(txtAd.Text) ? errorColor : validColor;
            };

            // Soyad validasyonu  
            txtSoyad.TextChanged += (s, ev) => {
                txtSoyad.BackColor = string.IsNullOrWhiteSpace(txtSoyad.Text) ? errorColor : validColor;
            };

            // Telefon validasyonu
            txtTelefon.TextChanged += (s, ev) => {
                txtTelefon.BackColor = string.IsNullOrWhiteSpace(txtTelefon.Text) || 
                                      txtTelefon.Text.Length < 11 ? errorColor : validColor;
            };

            // Oda listesi renklendirme
            cmbOda.DrawMode = DrawMode.OwnerDrawFixed;
            cmbOda.DrawItem += (s, ev) => {
                ev.DrawBackground();
                
                if (ev.Index >= 0)
                {
                    var row = ((DataRowView)cmbOda.Items[ev.Index]).Row;
                    string durum = row["durum"].ToString();
                    
                    // Oda durumuna göre renk belirleme
                    Color itemColor;
                    if (durum == "BOŞ")
                        itemColor = Color.LightGreen;
                    else if (durum == "DOLU")
                        itemColor = Color.LightPink;
                    else if (durum == "TEMİZLENİYOR")
                        itemColor = Color.LightYellow;
                    else
                        itemColor = SystemColors.Window;

                    // Arka plan boyama
                    using (var brush = new SolidBrush(itemColor))
                    {
                        ev.Graphics.FillRectangle(brush, ev.Bounds);
                    }

                    // Oda bilgisi yazma
                    string odaBilgisi = $"Oda {row["odaNumarasi"]} - {row["odaTipi"]} - {Convert.ToDecimal(row["temelFiyat"]):C}";
                    using (var brush = new SolidBrush(ev.ForeColor))
                    {
                        ev.Graphics.DrawString(odaBilgisi, ev.Font, brush, ev.Bounds);
                    }
                }
            };
        }

        private void OzetGuncelle()
        {
            // Özet panel ve başlık kontrollerini oluştur
            var pnlOzet = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            var lblOzetBaslik = new Label
            {
                Text = "REZERVASYON ÖZETİ",
                Font = new Font(this.Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };

            // Özet bilgilerini hazırla
            var ozetBilgileri = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(txtAd.Text) && !string.IsNullOrEmpty(txtSoyad.Text))
            {
                ozetBilgileri.Add("Müşteri", $"{txtAd.Text} {txtSoyad.Text}");
            }
            
            if (!string.IsNullOrEmpty(txtTC.Text))
            {
                ozetBilgileri.Add("TC Kimlik", txtTC.Text);
            }
            
            if (!string.IsNullOrEmpty(txtTelefon.Text))
            {
                ozetBilgileri.Add("Telefon", txtTelefon.Text);
            }
            
            ozetBilgileri.Add("Giriş Tarihi", dtpGiris.Value.ToShortDateString());
            ozetBilgileri.Add("Çıkış Tarihi", dtpCikis.Value.ToShortDateString());
            ozetBilgileri.Add("Toplam Gün", (dtpCikis.Value - dtpGiris.Value).Days.ToString());

            if (cmbOda.SelectedItem != null)
            {
                var selectedRow = ((DataRowView)cmbOda.SelectedItem).Row;
                ozetBilgileri.Add("Oda No", selectedRow["odaNumarasi"].ToString());
                ozetBilgileri.Add("Oda Tipi", selectedRow["odaTipi"].ToString());
                
                var gunlukFiyat = Convert.ToDecimal(selectedRow["temelFiyat"]);
                var gunSayisi = (dtpCikis.Value - dtpGiris.Value).Days;
                var toplamTutar = gunlukFiyat * gunSayisi;
                
                ozetBilgileri.Add("Toplam Tutar", toplamTutar.ToString("C"));
            }

            // Panel içeriğini oluştur
            pnlOzet.Controls.Clear();
            int row = 0;
            foreach (var bilgi in ozetBilgileri)
            {
                var lblKey = new Label
                {
                    Text = bilgi.Key + ":",
                    TextAlign = ContentAlignment.MiddleRight,
                    Dock = DockStyle.Fill,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                
                var lblValue = new Label
                {
                    Text = bilgi.Value,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Dock = DockStyle.Fill
                };

                pnlOzet.Controls.Add(lblKey, 0, row);
                pnlOzet.Controls.Add(lblValue, 1, row);
                row++;
            }

            // Özet paneli forma ekle
            this.Controls.Add(pnlOzet);
            this.Controls.Add(lblOzetBaslik);
        }

        private string FormatTelefonNo(string telefon)
        {
            // Sadece rakamları al
            var rakamlar = new string(telefon.Where(char.IsDigit).ToArray());
            
            // 11 haneden uzunsa kes
            if (rakamlar.Length > 11)
                rakamlar = rakamlar.Substring(0, 11);
        
            // Format uygula
            if (rakamlar.Length == 11)
            {
                return $"{rakamlar.Substring(0, 4)}-{rakamlar.Substring(4, 3)}-{rakamlar.Substring(7, 4)}";
            }
            
            return rakamlar;
        }

        // Telefon validasyonu için kullan
        private bool ValidateTelefon(string telefon)
        {
            var rakamlar = new string(telefon.Where(char.IsDigit).ToArray());
            return rakamlar.Length == 11 && rakamlar.StartsWith("05");  // 11 hane kontrolü
        }

        private void BtnRezervasyonIptal_Click(object sender, EventArgs e)
        {
            if (!_rezervasyonId.HasValue) return;

            var result = MessageBox.Show(
                "Bu rezervasyonu iptal etmek istediğinize emin misiniz?\nBu işlem geri alınamaz!",
                "Rezervasyon İptali",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    if (_rezervasyonDAL.DeleteRezervasyon(_rezervasyonId.Value))
                    {
                        MessageBox.Show(
                            "Rezervasyon başarıyla iptal edildi.",
                            "Başarılı",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        DialogResult = DialogResult.OK;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Rezervasyon iptal edilirken hata oluştu: {ex.Message}",
                        "Hata",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private bool ValidateForm()
        {
            // TC Kimlik, Ad, Soyad ve Telefon validasyonları
            if (string.IsNullOrEmpty(txtTC.Text) || txtTC.Text.Length != 11 || !txtTC.Text.All(char.IsDigit))
            {
                MessageBox.Show("TC Kimlik 11 haneli rakamlardan oluşmalıdır.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(txtAd.Text) || txtAd.Text.Length < 2)
            {
                MessageBox.Show("Ad en az 2 karakterden oluşmalıdır.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(txtSoyad.Text) || txtSoyad.Text.Length < 2)
            {
                MessageBox.Show("Soyad en az 2 karakterden oluşmalıdır.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(txtTelefon.Text) || txtTelefon.Text.Length < 11 || !txtTelefon.Text.StartsWith("05"))
            {
                MessageBox.Show("Telefon 11 haneli ve 05 ile başlamalıdır.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
    }
} 