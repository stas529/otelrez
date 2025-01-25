using System;
using System.Windows.Forms;
using System.Drawing;
using OtelRezervasyon.Models;
using OtelRezervasyon.Business;
using OtelRezervasyon.DataAccess;
using OtelRezervasyon.Presentation;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing.Text;
using System.IO;
using System.Text;  // StringBuilder için bunu ekledik
using iTextSharp.text;
using iTextSharp.text.pdf;
using SystemFont = System.Drawing.Font;
using SystemFontStyle = System.Drawing.FontStyle;
using SystemFontFamily = System.Drawing.FontFamily;
using PdfFont = iTextSharp.text.Font;   // iTextSharp.text.Font için alias
using System.Transactions;
using Font = System.Drawing.Font;  // Dosyanın başına ekle
using OtelRezervasyon.Extensions;  // Bunu ekledik

namespace OtelRezervasyon.Presentation
{
    public partial class YoneticiForm : Form
    {
        private TabControl tabControl;
        private TabPage tabMusteri;
        private TabPage tabOda;
        private TabPage tabRezervasyon;
        private TabPage tabFatura;

        private readonly MusteriDAL _musteriDAL;
        private readonly OdaDAL _odaDAL;
        private readonly RezervasyonDAL _rezervasyonDAL;
        private readonly FaturaDAL _faturaDAL;

        private DataGridView dgvMusteriler;
        private DataGridView dgvRezervasyonlar;
        private DataGridView dgvFaturalar;
        private Button btnEkle, btnGuncelle, btnSil;
        private Button btnRezervasyonYap;

        private class OdaListItem
        {
            public int OdaId { get; set; }
            public int OdaNumarasi { get; set; }
            public decimal TemelFiyat { get; set; }
            public string OdaTipi { get; set; }
            public string Ozellikler { get; set; }

            public override string ToString()
            {
                return $"Oda {OdaNumarasi}";
            }
        }

        public YoneticiForm()
        {
            InitializeComponent();
            
            this.Font = new SystemFont(SystemFonts.DefaultFont.FontFamily, 10f);
            
            _odaDAL = new OdaDAL();
            _musteriDAL = new MusteriDAL();
            _rezervasyonDAL = new RezervasyonDAL();
            _faturaDAL = new FaturaDAL();
            
            SetupForm();
        }

        private void SetupForm()
        {
            this.Size = new Size(1000, 600);  // 600x400'den 1000x600'e büyüttük
            
            tabControl = new TabControl { Dock = DockStyle.Fill };
            
            // Tab'leri oluştur
            tabMusteri = new TabPage("Müşteriler");
            tabOda = new TabPage("Odalar");
            tabRezervasyon = new TabPage("Rezervasyonlar");
            tabFatura = new TabPage("Faturalar");

            // Her tab'i ayarla
            SetupMusteriTab();
            SetupOdaTab();        // YENİ
            SetupRezervasyonTab(); // YENİ
            SetupFaturaTab();     // YENİ

            // Tab değişiminde verileri yenile
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                try 
                {
                    if (tabControl.SelectedTab == tabFatura && dgvFaturalar != null)
                    {
                        dgvFaturalar.DataSource = _faturaDAL?.GetAllFaturalar();
                    }
                    else if (tabControl.SelectedTab == tabRezervasyon && dgvRezervasyonlar != null)
                    {
                        dgvRezervasyonlar.DataSource = _rezervasyonDAL?.GetAllRezervasyonlar();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Veriler yüklenirken hata oluştu: {ex.Message}", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            tabControl.TabPages.AddRange(new TabPage[] { 
                tabMusteri, 
                tabOda, 
                tabRezervasyon,
                tabFatura 
            });

            this.Controls.Add(tabControl);
        }

        private void SetupMusteriTab()
        {
            // Panel oluştur (Dock = TOP)
            var buttonPanel = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 40,
                Padding = new Padding(5)
            };

            // Butonları oluştur ve stilleri uygula
            btnEkle = new Button 
            { 
                Text = "Müşteri Ekle",
                Width = 160,  // Genişliği artırdık
                Location = new Point(5, 5)
            };
            btnEkle.ApplyCustomStyle("primary");  // Mavi renk

            btnGuncelle = new Button 
            { 
                Text = "Müşteri Düzenle",
                Width = 160,  // Genişliği artırdık
                Location = new Point(170, 5)
            };
            btnGuncelle.ApplyCustomStyle("info");  // Açık mavi

            btnSil = new Button 
            { 
                Text = "Müşteri Sil",
                Width = 160,  // Genişliği artırdık
                Location = new Point(335, 5)
            };
            btnSil.ApplyCustomStyle("danger");  // Kırmızı

            // DataGridView (Dock = FILL)
            dgvMusteriler = new DataGridView
            {
                Dock = DockStyle.Fill,  // BURASI ÖNEMLİ
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Önce butonları panele ekle
            buttonPanel.Controls.AddRange(new Control[] { btnEkle, btnGuncelle, btnSil });

            // Sonra panel ve grid'i tab'e ekle (SIRAYLA!)
            tabMusteri.Controls.Add(dgvMusteriler);  // Önce grid (FILL)
            tabMusteri.Controls.Add(buttonPanel);    // Sonra panel (TOP)

            // Event'leri bağla
            btnEkle.Click += BtnEkle_Click;
            btnGuncelle.Click += BtnGuncelle_Click;
            btnSil.Click += BtnSil_Click;

            // Verileri yükle
            dgvMusteriler.DataSource = _musteriDAL.GetAllMusteriler();
        }

        private void BtnEkle_Click(object sender, EventArgs e)
        {
            var musteriForm = new MusteriForm(); // Yeni form
            if (musteriForm.ShowDialog() == DialogResult.OK)
            {
                dgvMusteriler.DataSource = _musteriDAL.GetAllMusteriler();
            }
        }

        private void BtnGuncelle_Click(object sender, EventArgs e)
        {
            if (dgvMusteriler.SelectedRows.Count == 0) return;

            var musteriId = Convert.ToInt32(dgvMusteriler.SelectedRows[0].Cells["musteriId"].Value);
            var musteri = new Musteri
            {
                MusteriId = musteriId,
                TcKimlik = dgvMusteriler.SelectedRows[0].Cells["tcKimlik"].Value.ToString(),
                Ad = dgvMusteriler.SelectedRows[0].Cells["ad"].Value.ToString(),
                Soyad = dgvMusteriler.SelectedRows[0].Cells["soyad"].Value.ToString(),
                Email = dgvMusteriler.SelectedRows[0].Cells["email"].Value.ToString(),
                Telefon = dgvMusteriler.SelectedRows[0].Cells["telefon"].Value.ToString(),
                Adres = dgvMusteriler.SelectedRows[0].Cells["adres"].Value.ToString()
            };

            var musteriForm = new MusteriForm(musteri); // Mevcut müşteriyi gönder
            if (musteriForm.ShowDialog() == DialogResult.OK)
            {
                dgvMusteriler.DataSource = _musteriDAL.GetAllMusteriler();
            }
        }

        private void BtnSil_Click(object sender, EventArgs e)
        {
            if (dgvMusteriler.SelectedRows.Count == 0) return;

            var musteriId = Convert.ToInt32(dgvMusteriler.SelectedRows[0].Cells["musteriId"].Value);
            if (_musteriDAL.DeleteMusteri(musteriId))
            {
                dgvMusteriler.DataSource = _musteriDAL.GetAllMusteriler();
            }
        }

        // Hızlıca diğer tab'leri ekleyelim
        private void SetupOdaTab()
        {
            // Panel oluştur
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            // Butonları oluştur ve stilleri uygula
            var btnOdaEkle = new Button
            {
                Text = "Oda Ekle",
                Width = 160,
                Location = new Point(5, 5)
            };
            btnOdaEkle.ApplyCustomStyle("primary");  // Mavi

            var btnOdaDuzenle = new Button
            {
                Text = "Oda Düzenle",
                Width = 160,
                Location = new Point(170, 5)
            };
            btnOdaDuzenle.ApplyCustomStyle("info");  // Açık mavi

            var btnOdaSil = new Button
            {
                Text = "Oda Sil",
                Width = 160,
                Location = new Point(335, 5)
            };
            btnOdaSil.ApplyCustomStyle("danger");  // Kırmızı

            // DataGridView
            var dgvOdalar = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,  // Fill yerine None yaptık
                ReadOnly = true
            };

            // Kolonların genişliklerini ayarla
            dgvOdalar.DataBindingComplete += (s, e) => {
                if (dgvOdalar.Columns.Count > 0)
                {
                    if (dgvOdalar.Columns.Contains("Oda ID")) 
                        dgvOdalar.Columns["Oda ID"].Width = 70;
                    
                    if (dgvOdalar.Columns.Contains("Oda No"))
                        dgvOdalar.Columns["Oda No"].Width = 80;
                    
                    if (dgvOdalar.Columns.Contains("Fiyat"))
                    {
                        dgvOdalar.Columns["Fiyat"].Width = 100;
                        dgvOdalar.Columns["Fiyat"].DefaultCellStyle.Format = "N2";
                        dgvOdalar.Columns["Fiyat"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                    
                    if (dgvOdalar.Columns.Contains("Durum"))
                        dgvOdalar.Columns["Durum"].Width = 80;
                    
                    if (dgvOdalar.Columns.Contains("Oda Tipi"))
                        dgvOdalar.Columns["Oda Tipi"].Width = 100;
                    
                    if (dgvOdalar.Columns.Contains("Açıklama"))
                    {
                        dgvOdalar.Columns["Açıklama"].Width = 200;
                        dgvOdalar.Columns["Açıklama"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }
                    
                    if (dgvOdalar.Columns.Contains("Özellikler"))
                    {
                        dgvOdalar.Columns["Özellikler"].Width = 300;
                        dgvOdalar.Columns["Özellikler"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }
                }
            };

            // Event handlers
            btnOdaEkle.Click += (s, e) => {
                var form = new Form
                {
                    Text = "Yeni Oda",
                    Size = new Size(400, 450),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    Padding = new Padding(10),
                    Font = new SystemFont(SystemFonts.DefaultFont.FontFamily, 10f)  // Yeni oda formunun fontunu da büyüt
                };

                // Temel kontroller
                var txtOdaNo = new TextBox { Left = 120, Top = 20, Width = 180 };
                var txtFiyat = new TextBox { Left = 120, Top = 50, Width = 180, Text = "1500.00" };
                
                var lblFiyatBilgi = new Label 
                { 
                    Text = "Özel oda özellikleri seçildiğinde fiyat otomatik artacaktır.", 
                    Left = 120, 
                    Top = 75,
                    ForeColor = Color.Gray,
                    Font = new SystemFont(SystemFonts.DefaultFont.FontFamily, 9f, SystemFontStyle.Italic),  // Bilgi yazısı biraz daha küçük
                    AutoSize = true
                };

                var lblOnerilenFiyat = new Label 
                { 
                    Left = 120, 
                    Top = 290,
                    Width = 250,  // Genişlik ekledik
                    ForeColor = Color.Green,
                    Font = new SystemFont(SystemFonts.DefaultFont, SystemFontStyle.Bold),
                    AutoSize = false,  // AutoSize'ı kapattık
                    Visible = false
                };

                // Özel oda checkbox'ı
                var chkOzelOda = new CheckBox 
                { 
                    Text = "Özel Oda Özellikleri",
                    Left = 20, 
                    Top = 90,
                    AutoSize = true
                };

                // Özel oda özellikleri (başlangıçta gizli)
                var txtAciklama = new TextBox { Left = 120, Top = 130, Width = 180, Visible = false };
                var chkCiftYatak = new CheckBox { Text = "Çift Yataklı", Left = 120, Top = 170, AutoSize = true, Visible = false };
                var chkGenisBalkon = new CheckBox { Text = "Geniş Balkon", Left = 120, Top = 200, AutoSize = true, Visible = false };
                var chkJakuzi = new CheckBox { Text = "Jakuzi", Left = 120, Top = 230, AutoSize = true, Visible = false };
                var chkManzara = new CheckBox { Text = "Şehir Manzaralı", Left = 120, Top = 260, AutoSize = true, Visible = false };
                
                var lblAciklama = new Label { Text = "Açıklama:", Left = 20, Top = 133, Visible = false };
                var lblOzellikler = new Label { Text = "Özellikler:", Left = 20, Top = 170, Visible = false };

                // Fiyat hesaplama fonksiyonu güncellendi
                void FiyatOnerisiGuncelle()
                {
                    // Fiyat string'ini doğru şekilde parse et
                    decimal temelFiyat;
                    if (!decimal.TryParse(txtFiyat.Text, 
                        NumberStyles.Any, 
                        CultureInfo.InvariantCulture, 
                        out temelFiyat))
                    {
                        txtFiyat.Text = "1500";
                        temelFiyat = 1500;
                    }

                    if (!chkOzelOda.Checked)
                    {
                        lblOnerilenFiyat.Visible = false;
                        return;
                    }

                    decimal toplamArtis = 0m;
                    var artislar = new List<string>();

                    if (chkCiftYatak.Checked)
                    {
                        toplamArtis += 0.3m;
                        artislar.Add("Çift Yatak +%30");
                    }
                    if (chkGenisBalkon.Checked)
                    {
                        toplamArtis += 0.15m;
                        artislar.Add("Geniş Balkon +%15");
                    }
                    if (chkJakuzi.Checked)
                    {
                        toplamArtis += 0.2m;
                        artislar.Add("Jakuzi +%20");
                    }
                    if (chkManzara.Checked)
                    {
                        toplamArtis += 0.1m;
                        artislar.Add("Şehir Manzara +%10");
                    }

                    decimal oneriFiyat = temelFiyat * (1 + toplamArtis);
                    
                    // Türk Lirası formatında göster
                    var turkishCulture = new CultureInfo("tr-TR");
                    string formatlanmisFiyat = oneriFiyat.ToString("N0", turkishCulture);
                    
                    lblOnerilenFiyat.Text = $"Önerilen Fiyat: ₺{formatlanmisFiyat}\r\n" + 
                                           $"Toplam Artış: %{(toplamArtis * 100):0}\r\n" +
                                           (artislar.Any() ? $"Detay: {string.Join(",\r\n", artislar)}" : "");
                    lblOnerilenFiyat.Height = TextRenderer.MeasureText(lblOnerilenFiyat.Text, lblOnerilenFiyat.Font, 
                        new Size(lblOnerilenFiyat.Width, 0), 
                        TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
                    lblOnerilenFiyat.Visible = true;
                }

                // TextBox'a sadece sayı girilmesine izin ver
                txtFiyat.KeyPress += (sender2, e2) => {
                    if (!char.IsControl(e2.KeyChar) && !char.IsDigit(e2.KeyChar))
                    {
                        e2.Handled = true;
                    }
                };

                // Kaydet butonu - daha aşağıya
                var btnKaydet = new Button 
                { 
                    Text = "Kaydet", 
                    Left = 120, 
                    Top = 350,  // 320'den 350'ye indirdik
                    Width = 100,
                    Height = 30
                };

                btnKaydet.Click += (se, ev) => {
                    try
                    {
                        if (string.IsNullOrEmpty(txtOdaNo.Text) || string.IsNullOrEmpty(txtFiyat.Text))
                        {
                            MessageBox.Show("Oda no ve fiyat alanları boş bırakılamaz!");
                            return;
                        }

                        // Oda numarası kontrolü
                        int yeniOdaNo = int.Parse(txtOdaNo.Text);
                        if (yeniOdaNo <= 0)
                        {
                            MessageBox.Show("Oda numarası 0'dan büyük olmalıdır!");
                            return;
                        }

                        // YENİ ODA İÇİN KONTROL - Sadece numara var mı diye bak
                        var mevcutOdalar = _odaDAL.GetAllOdalar();
                        foreach (DataRow mevcutRow in mevcutOdalar.Rows)
                        {
                            if (Convert.ToInt32(mevcutRow["Oda No"]) == yeniOdaNo)
                            {
                                MessageBox.Show($"{yeniOdaNo} numaralı oda zaten mevcut! Başka bir numara seçin.");
                                return;
                            }
                        }

                        // Fiyat kontrolü
                        decimal yeniFiyat = decimal.Parse(txtFiyat.Text, CultureInfo.InvariantCulture);
                        if (yeniFiyat <= 0)
                        {
                            MessageBox.Show("Fiyat 0'dan büyük olmalıdır!");
                            return;
                        }

                        // Oda ekleme işlemleri...
                        if (chkOzelOda.Checked)
                        {
                            var ozelOda = new OzelOda
                            {
                                OdaNumarasi = yeniOdaNo,
                                TemelFiyat = yeniFiyat,
                                Durum = "BOS",
                                Aciklama = txtAciklama.Text,
                                CiftYatakli = chkCiftYatak.Checked,
                                GenisBalkon = chkGenisBalkon.Checked,
                                Jakuzi = chkJakuzi.Checked,
                                SehirManzara = chkManzara.Checked
                            };

                            if (_odaDAL.AddOzelOda(ozelOda))
                            {
                                MessageBox.Show("Özel oda başarıyla eklendi!");
                                dgvOdalar.DataSource = _odaDAL.GetAllOdalar();
                                form.Close();
                            }
                        }
                        else
                        {
                            var yeniOda = new Oda
                            {
                                OdaNumarasi = yeniOdaNo,
                                TemelFiyat = yeniFiyat,
                                Durum = "BOS"
                            };

                            if (_odaDAL.AddOda(yeniOda))
                            {
                                MessageBox.Show("Standart oda başarıyla eklendi!");
                                dgvOdalar.DataSource = _odaDAL.GetAllOdalar();
                                form.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hata oluştu: " + ex.Message);
                    }
                };

                // Özel oda checkbox'ı değişince
                chkOzelOda.CheckedChanged += (sender, args) => {
                    // Özel oda özelliklerinin görünürlüğünü ayarla
                    txtAciklama.Visible = chkOzelOda.Checked;
                    lblAciklama.Visible = chkOzelOda.Checked;
                    lblOzellikler.Visible = chkOzelOda.Checked;
                    chkCiftYatak.Visible = chkOzelOda.Checked;
                    chkGenisBalkon.Visible = chkOzelOda.Checked;
                    chkJakuzi.Checked = chkOzelOda.Checked;
                    chkManzara.Checked = chkOzelOda.Checked;
                    lblOnerilenFiyat.Visible = chkOzelOda.Checked;

                    // Checkbox işaretli değilse önerilen fiyatı gizle
                    if (!chkOzelOda.Checked)
                    {
                        lblOnerilenFiyat.Visible = false;
                        // Checkbox'ları sıfırla
                        chkCiftYatak.Checked = false;
                        chkGenisBalkon.Checked = false;
                        chkJakuzi.Checked = false;
                        chkManzara.Checked = false;
                        txtAciklama.Text = "";
                    }
                    else
                    {
                        // Checkbox işaretlenince fiyatı güncelle
                        FiyatOnerisiGuncelle();
                    }
                };

                // Her özellik checkbox'ı için event ekle
                foreach(var chk in new[] { chkCiftYatak, chkGenisBalkon, chkJakuzi, chkManzara })
                {
                    chk.CheckedChanged += (sender, args) => FiyatOnerisiGuncelle();
                }

                // Temel fiyat değişince
                txtFiyat.TextChanged += (sender, args) => FiyatOnerisiGuncelle();

                // Kontrolleri forma ekle
                form.Controls.AddRange(new Control[] {
                    new Label { Text = "Oda No:", Left = 20, Top = 23 },
                    new Label { Text = "Fiyat (₺):", Left = 20, Top = 53 },
                    txtOdaNo, 
                    txtFiyat,
                    lblFiyatBilgi,
                    chkOzelOda,
                    lblAciklama,
                    txtAciklama,
                    lblOzellikler,
                    chkCiftYatak,
                    chkGenisBalkon,
                    chkJakuzi,
                    chkManzara,
                    lblOnerilenFiyat,
                    btnKaydet
                });

                form.ShowDialog();
            };

            btnOdaDuzenle.Click += (s, e) => {
                if (dgvOdalar.SelectedRows.Count == 0) return;

                var row = dgvOdalar.SelectedRows[0];
                var odaId = Convert.ToInt32(row.Cells["OdaId"].Value);
                var odaNumarasi = Convert.ToInt32(row.Cells["OdaNumarasi"].Value);
                var temelFiyat = Convert.ToDecimal(row.Cells["TemelFiyat"].Value);
                var durum = row.Cells["Durum"].Value.ToString();

                // Düzenleme formu - boyutu büyüttük
                var form = new Form
                {
                    Text = "Oda Düzenle",
                    Size = new Size(500, 320),  // Genişlik ve yüksekliği artırdık
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Font = new SystemFont(SystemFonts.DefaultFont.FontFamily, 10f)
                };

                // Kontrollerin konumlarını ayarla
                var lblDurum = new Label { Text = "Durum:", Left = 50, Top = 50, AutoSize = true };
                var cmbDurum = new ComboBox
                {
                    Location = new Point(150, 47),
                    Width = 250,  // Genişliği artırdık
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbDurum.Items.AddRange(new[] { "BOS", "DOLU", "TEMIZLIK", "BAKIM" });
                cmbDurum.SelectedItem = durum;

                var lblFiyat = new Label { Text = "Fiyat (₺):", Left = 50, Top = 100, AutoSize = true };
                var txtFiyat = new TextBox 
                { 
                    Location = new Point(150, 97),
                    Width = 250,  // Genişliği artırdık
                    Text = temelFiyat.ToString("0")
                };

                // Fiyat bilgi label'ı
                var lblFiyatBilgi = new Label
                {
                    Text = "Not: Fiyat değişiklikleri mevcut rezervasyonları etkilemez.",
                    Left = 150,
                    Top = 130,
                    ForeColor = Color.Gray,
                    Font = new SystemFont(SystemFonts.DefaultFont.FontFamily, 9f, SystemFontStyle.Italic),
                    AutoSize = true
                };

                // Butonların konumlarını ayarla
                var btnKaydet = new Button 
                { 
                    Text = "Kaydet", 
                    Location = new Point(150, 200),  // Y koordinatını artırdık
                    Width = 150,  // Genişliği artırdık
                    Height = 40,  // Yüksekliği artırdık
                    DialogResult = DialogResult.OK
                };

                var btnIptal = new Button
                {
                    Text = "İptal",
                    Location = new Point(310, 200),  // X ve Y koordinatlarını ayarladık
                    Width = 90,  // Genişliği artırdık
                    Height = 40,  // Yüksekliği artırdık
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { 
                    lblDurum, cmbDurum, 
                    lblFiyat, txtFiyat,
                    lblFiyatBilgi,
                    btnKaydet,
                    btnIptal
                });

                form.AcceptButton = btnKaydet;
                form.CancelButton = btnIptal;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Fiyat kontrolü
                        if (!decimal.TryParse(txtFiyat.Text, out decimal yeniFiyat) || yeniFiyat <= 0)
                        {
                            MessageBox.Show("Geçerli bir fiyat giriniz!");
                            return;
                        }

                        string yeniDurum = cmbDurum.SelectedItem.ToString();
                        
                        // Oda bilgilerini güncelle
                        var oda = _odaDAL.GetOdaById(odaId);
                        if (oda != null)
                        {
                            oda.TemelFiyat = yeniFiyat;
                            oda.Durum = yeniDurum;
                            
                            if (_odaDAL.UpdateOda(oda))
                            {
                                MessageBox.Show("Oda bilgileri güncellendi!");
                                dgvOdalar.DataSource = _odaDAL.GetAllOdalar();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hata oluştu: " + ex.Message);
                    }
                }
            };

            btnOdaSil.Click += (s, e) => {
                if (dgvOdalar.SelectedRows.Count == 0) return;

                var row = dgvOdalar.SelectedRows[0];
                var odaId = Convert.ToInt32(row.Cells["OdaId"].Value);

                if (MessageBox.Show("Odayı silmek istediğinize emin misiniz?", "Onay", 
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (_odaDAL.DeleteOda(odaId))
                    {
                        MessageBox.Show("Oda silindi");
                        dgvOdalar.DataSource = _odaDAL.GetAllOdalar();
                    }
                }
            };

            // Kontrolleri ekle
            buttonPanel.Controls.AddRange(new Control[] { btnOdaEkle, btnOdaDuzenle, btnOdaSil });
            tabOda.Controls.Add(dgvOdalar);
            tabOda.Controls.Add(buttonPanel);

            // Verileri yükle
            dgvOdalar.DataSource = _odaDAL.GetAllOdalar();
        }

        private void SetupRezervasyonTab()
        {
            // Panel oluştur
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            // Butonları oluştur
            btnRezervasyonYap = new Button
            {
                Text = "Yeni Rezervasyon",
                Width = 160,
                Location = new Point(5, 5)
            };
            btnRezervasyonYap.ApplyCustomStyle("primary");  // Mavi

            var btnRezervasyonGoruntule = new Button
            {
                Text = "Rezervasyon Detay",
                Width = 160,
                Location = new Point(170, 5)
            };
            btnRezervasyonGoruntule.ApplyCustomStyle("info");  // Açık mavi

            var btnRezervasyonIptal = new Button
            {
                Text = "Rezervasyonu İptal Et",
                Width = 175,
                Location = new Point(335, 5)
            };
            btnRezervasyonIptal.ApplyCustomStyle("danger");  // Sadece bu kalsın

            // DataGridView
            dgvRezervasyonlar = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };

            // İptal butonu click eventi
            btnRezervasyonIptal.Click += (s, e) =>
            {
                if (dgvRezervasyonlar.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Lütfen iptal edilecek rezervasyonu seçin!", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var rezervasyonId = Convert.ToInt32(dgvRezervasyonlar.SelectedRows[0].Cells["RezervasyonId"].Value);
                
                var result = MessageBox.Show(
                    "Bu rezervasyonu iptal etmek istediğinize emin misiniz?\nBu işlem geri alınamaz!",
                    "Rezervasyon İptali",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        if (_rezervasyonDAL.DeleteRezervasyon(rezervasyonId))
                        {
                            MessageBox.Show(
                                "Rezervasyon başarıyla iptal edildi.",
                                "Başarılı",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            dgvRezervasyonlar.DataSource = _rezervasyonDAL.GetAllRezervasyonlar();
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
            };

            // Event handlers
            btnRezervasyonYap.Click += (s, e) => 
            {
                using (var rezervasyonForm = new RezervasyonForm())
                {
                    if (rezervasyonForm.ShowDialog() == DialogResult.OK)
                    {
                        dgvRezervasyonlar.DataSource = _rezervasyonDAL.GetAllRezervasyonlar();
                    }
                }
            };

            btnRezervasyonGoruntule.Click += (s, e) =>
            {
                if (dgvRezervasyonlar.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Lütfen bir rezervasyon seçin!", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var rezervasyonId = Convert.ToInt32(dgvRezervasyonlar.SelectedRows[0].Cells["RezervasyonId"].Value);
                using (var rezervasyonForm = new RezervasyonForm(rezervasyonId))
                {
                    if (rezervasyonForm.ShowDialog() == DialogResult.OK)
                    {
                        dgvRezervasyonlar.DataSource = _rezervasyonDAL.GetAllRezervasyonlar();
                    }
                }
            };

            // Çift tıklama ile detay açma
            dgvRezervasyonlar.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var rezervasyonId = Convert.ToInt32(dgvRezervasyonlar.Rows[e.RowIndex].Cells["RezervasyonId"].Value);
                    using (var rezervasyonForm = new RezervasyonForm(rezervasyonId))
                    {
                        if (rezervasyonForm.ShowDialog() == DialogResult.OK)
                        {
                            dgvRezervasyonlar.DataSource = _rezervasyonDAL.GetAllRezervasyonlar();
                        }
                    }
                }
            };

            // Kontrolleri panele ekle
            buttonPanel.Controls.AddRange(new Control[] { 
                btnRezervasyonYap, 
                btnRezervasyonGoruntule,
                btnRezervasyonIptal 
            });

            // Kontrolleri tab'e ekle
            tabRezervasyon.Controls.Add(dgvRezervasyonlar);
            tabRezervasyon.Controls.Add(buttonPanel);

            // İlk veri yüklemesi
            dgvRezervasyonlar.DataSource = _rezervasyonDAL.GetAllRezervasyonlar();
        }

        private void SetupFaturaTab()
        {
            // DataGridView'ı en başta tanımla
            dgvFaturalar = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            var btnFaturaGoster = new Button
            {
                Text = "Fatura Detay",
                Width = 110,
                Location = new Point(5, 5)
            };
            btnFaturaGoster.ApplyCustomStyle("info");

            var btnPdfExport = new Button
            {
                Text = "PDF Kaydet",
                Width = 100,
                Location = new Point(130, 5)
            };
            btnPdfExport.ApplyCustomStyle("success");

            var btnFaturaSil = new Button
            {
                Text = "Fatura Sil",
                Width = 100,
                Location = new Point(240, 5)
            };
            btnFaturaSil.ApplyCustomStyle("danger");

            var btnYenile = new Button
            {
                Text = "↻ Yenile",
                Width = 100,
                Location = new Point(350, 5)
            };
            btnYenile.ApplyCustomStyle("primary");

            // Event handlers...
            btnYenile.Click += (s, e) =>
            {
                try
                {
                    dgvFaturalar.DataSource = _faturaDAL.GetAllFaturalar();
                    MessageBox.Show("Fatura listesi yenilendi.", "Bilgi", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Faturalar yüklenirken hata: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnFaturaGoster.Click += (s, e) => {
                if (dgvFaturalar.SelectedRows.Count == 0) return;

                var faturaId = Convert.ToInt32(dgvFaturalar.SelectedRows[0].Cells["faturaId"].Value);
                var fatura = _faturaDAL.GetFaturaById(faturaId);
                
                if (fatura == null) return;

                var detayForm = new Form
                {
                    Text = $"Fatura Detay - No: {faturaId}",
                    Size = new Size(500, 600),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var txtDetay = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new SystemFont("Consolas", 10),
                    ScrollBars = ScrollBars.Vertical
                };

                // Fatura detay formatı
                txtDetay.Text = $@"
═══════════════════════════════════
        OTEL ADI FATURA
═══════════════════════════════════

Fatura No: {fatura.faturaId}
Tarih: {fatura.odemeTarihi:dd/MM/yyyy HH:mm}

MÜŞTERİ BİLGİLERİ
────────────────────────
Ad Soyad: {fatura.musteriAdSoyad}
TC Kimlik: {fatura.musteriTC}

KONAKLAMA BİLGİLERİ
────────────────────────
Oda No: {fatura.odaNo}
Giriş: {fatura.girisTarihi:dd/MM/yyyy}
Çıkış: {fatura.cikisTarihi:dd/MM/yyyy}
Süre: {(fatura.cikisTarihi - fatura.girisTarihi).Days} gün

ÜCRET DETAYI
────────────────────────
Günlük Ücret: {fatura.gunlukUcret:C2}
Toplam Konaklama: {fatura.toplamFiyat:C2}
Ekstra Hizmetler: {fatura.ekstraHizmetler:C2}
────────────────────────
GENEL TOPLAM: {fatura.toplamFiyat:C2}

Ödeme Durumu: {fatura.odemeDurumu}
Ödeme Tipi: {fatura.odemeTipi}

═══════════════════════════════════
";

                detayForm.Controls.Add(txtDetay);
                detayForm.ShowDialog();
            };

            btnPdfExport.Click += (s, e) => {
                if (dgvFaturalar.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Lütfen bir fatura seçin!", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var faturaId = Convert.ToInt32(dgvFaturalar.SelectedRows[0].Cells["faturaId"].Value);
                    var fatura = _faturaDAL.GetFaturaById(faturaId);
                    
                    if (fatura == null)
                    {
                        MessageBox.Show("Fatura bilgileri alınamadı!", "Hata", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "PDF Dosyası|*.pdf",
                        FileName = $"Fatura_{faturaId}_{DateTime.Now:yyyyMMdd}.pdf",
                        Title = "Faturayı PDF Olarak Kaydet"
                    };

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            ExportToPdf(fatura, saveDialog.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"PDF oluşturulurken hata:\n{ex.Message}",
                                "Hata",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"İşlem sırasında hata oluştu:\n{ex.Message}",
                        "Hata",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            btnFaturaSil.Click += (s, e) => {
                if (dgvFaturalar.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Lütfen silinecek faturayı seçin!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var faturaId = Convert.ToInt32(dgvFaturalar.SelectedRows[0].Cells["faturaId"].Value);
                var odemeDurumu = dgvFaturalar.SelectedRows[0].Cells["odemeDurumu"].Value.ToString();

                if (odemeDurumu == "ODENDI")
                {
                    MessageBox.Show("Ödenmiş faturalar silinemez!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "Fatura silinecek! Bu işlem geri alınamaz.\nDevam etmek istiyor musunuz?",
                    "Fatura Sil",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    if (_faturaDAL.DeleteFatura(faturaId))
                    {
                        MessageBox.Show("Fatura başarıyla silindi.", "Başarılı",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dgvFaturalar.DataSource = _faturaDAL.GetAllFaturalar();
                    }
                }
            };

            // Kontrolleri ekle
            buttonPanel.Controls.AddRange(new Control[] { 
                btnFaturaGoster, 
                btnPdfExport, 
                btnFaturaSil,
                btnYenile
            });
            
            tabFatura.Controls.Add(dgvFaturalar);
            tabFatura.Controls.Add(buttonPanel);

            // İlk veri yüklemesi
            dgvFaturalar.DataSource = _faturaDAL.GetAllFaturalar();
        }

        private void ExportToPdf(Fatura fatura, string path)
        {
            var doc = new iTextSharp.text.Document(PageSize.A4, 50, 50, 50, 50);
            
            try
            {
                var writer = PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
                doc.Open();

                // Fontları tanımla
                var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                var normalFont = new iTextSharp.text.Font(baseFont, 11);
                var boldFont = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.BOLD);
                var headerFont = new iTextSharp.text.Font(baseFont, 18, iTextSharp.text.Font.BOLD);

                // Başlık
                var header = new Paragraph("OTEL FATURASI", headerFont);
                header.Alignment = Element.ALIGN_CENTER;
                header.SpacingAfter = 20f;
                doc.Add(header);

                // İçerik
                var content = new StringBuilder();
                content.AppendLine($"Fatura No: {fatura.faturaId}");
                content.AppendLine($"Tarih: {fatura.odemeTarihi:dd/MM/yyyy}\n");
                
                content.AppendLine("MUSTERI BILGILERİ");
                content.AppendLine("------------------------");
                content.AppendLine($"Ad Soyad: {fatura.musteriAdSoyad}");
                content.AppendLine($"TC: {fatura.musteriTC}\n");
                
                content.AppendLine("KONAKLAMA BILGILERİ");
                content.AppendLine("------------------------");
                content.AppendLine($"Oda No: {fatura.odaNo}");
                content.AppendLine($"Giris: {fatura.girisTarihi:dd/MM/yyyy}");
                content.AppendLine($"Cikis: {fatura.cikisTarihi:dd/MM/yyyy}");
                content.AppendLine($"Sure: {(fatura.cikisTarihi - fatura.girisTarihi).Days} gun\n");
                
                content.AppendLine("UCRET DETAYI");
                content.AppendLine("------------------------");
                content.AppendLine($"Gunluk Ucret: {fatura.gunlukUcret:C2}");
                content.AppendLine($"Toplam: {fatura.toplamFiyat:C2}\n");
                
                var paragraph = new Paragraph(content.ToString(), normalFont);
                doc.Add(paragraph);

                // Ödeme Durumu
                var odemeDurumu = new Paragraph("ODENDI", boldFont);
                odemeDurumu.Alignment = Element.ALIGN_RIGHT;
                doc.Add(odemeDurumu);

                doc.Close();

                // PDF'i açmak isteyip istemediğini sor
                var result = MessageBox.Show(
                    "PDF dosyası oluşturuldu. Şimdi açmak ister misiniz?",
                    "PDF Oluşturuldu",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF oluşturulurken hata: {ex.Message}");
            }
        }

        // TC Kimlik kontrolü için metod ekle
        private bool TcKimlikKontrol(string tcKimlik) 
        {
            // 11 haneli olmalı ve sadece rakam içermeli
            if (tcKimlik.Length != 11 || !tcKimlik.All(char.IsDigit))
                return false;

            // İlk rakam 0 olamaz
            if (tcKimlik[0] == '0')
                return false;

            return true;
        }

        // Form validasyonu için metod
        private void ValidateFormLocal(TextBox txtTC, TextBox txtAd, TextBox txtSoyad, 
            TextBox txtTelefon, DateTimePicker dtpGiris, DateTimePicker dtpCikis, 
            ListBox cmbOda, TextBox txtOzet)
        {
            bool isValid = true;

            if (txtTC.Text.Length != 11 || !txtTC.Text.All(char.IsDigit))
            {
                txtTC.BackColor = Color.MistyRose;
                isValid = false;
            }
            else
            {
                txtTC.BackColor = Color.White;
            }

            if (string.IsNullOrWhiteSpace(txtAd.Text))
            {
                txtAd.BackColor = Color.MistyRose;
                isValid = false;
            }
            else
            {
                txtAd.BackColor = Color.White;
            }

            if (string.IsNullOrWhiteSpace(txtSoyad.Text))
            {
                txtSoyad.BackColor = Color.MistyRose;
                isValid = false;
            }
            else
            {
                txtSoyad.BackColor = Color.White;
            }

            string phone = txtTelefon.Text.Replace("-", "");
            if (phone.Length != 10 || !phone.All(char.IsDigit))
            {
                txtTelefon.BackColor = Color.MistyRose;
                isValid = false;
            }
            else
            {
                txtTelefon.BackColor = Color.White;
            }

            if (dtpCikis.Value <= dtpGiris.Value)
            {
                dtpCikis.CalendarTitleBackColor = Color.MistyRose;
                isValid = false;
            }
            else
            {
                dtpCikis.CalendarTitleBackColor = SystemColors.Window;
            }

            if (cmbOda.SelectedItem == null)
            {
                isValid = false;
            }

            txtOzet.Text = "";
            UpdateOzet(txtTC, txtAd, txtSoyad, txtTelefon, dtpGiris, dtpCikis, cmbOda, txtOzet);

            btnRezervasyonYap.Enabled = isValid;
        }

        // Özet güncelleme metodu
        private void UpdateOzet(TextBox txtTC, TextBox txtAd, TextBox txtSoyad, 
            TextBox txtTelefon, DateTimePicker dtpGiris, DateTimePicker dtpCikis, 
            ListBox cmbOda, TextBox txtOzet)
        {
            if (cmbOda.SelectedItem != null)
            {
                var odaRow = ((DataRowView)cmbOda.SelectedItem).Row;
                var gunSayisi = (dtpCikis.Value - dtpGiris.Value).Days;
                var fiyat = Convert.ToDecimal(odaRow["temelFiyat"]);
                var toplamTutar = fiyat * gunSayisi;

                txtOzet.Text = $@"REZERVASYON ÖZETİ

MÜŞTERİ BİLGİLERİ                           KONAKLAMA BİLGİLERİ
TC Kimlik : {txtTC.Text,-15}                Oda No     : {odaRow["odaNumarasi"]}
Ad        : {txtAd.Text,-15}                Oda Tipi   : {odaRow["odaTipi"]}
Soyad     : {txtSoyad.Text,-15}                Özellikler : {odaRow["ozellikler"]}
Telefon   : {txtTelefon.Text,-15}                Giriş      : {dtpGiris.Value:dd.MM.yyyy}
                                            Çıkış      : {dtpCikis.Value:dd.MM.yyyy}
                                            Süre       : {gunSayisi} gün

ÜCRET DETAYI
Günlük    : {fiyat:N2} ₺
Toplam    : {toplamTutar:N2} ₺";
            }
        }

        // Oda listesi için özel çizim
        private void DrawOdaListItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var cmbOda = (ListBox)sender;
            var odaRow = ((DataRowView)cmbOda.Items[e.Index]).Row;
            
            // Seçili öğe için farklı arka plan rengi
            Color renk = e.State.HasFlag(DrawItemState.Selected) ? 
                Color.FromArgb(200, 230, 200) : Color.LightGreen;

            using (var brush = new SolidBrush(renk))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Seçili öğe için kenarlık çiz
            if (e.State.HasFlag(DrawItemState.Selected))
            {
                using (var pen = new Pen(Color.DarkGreen, 2))
                {
                    var rect = e.Bounds;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            var odaNo = odaRow["odaNumarasi"].ToString();
            var fiyat = Convert.ToDecimal(odaRow["temelFiyat"]);
            var odaTipi = odaRow["odaTipi"].ToString();
            var ozellikler = odaRow["ozellikler"].ToString();

            using (var boldFont = new System.Drawing.Font(e.Font.FontFamily, 10, FontStyle.Bold))
            using (var normalFont = new System.Drawing.Font(e.Font.FontFamily, 9))
            {
                // Birinci satır - Oda No ve Fiyat
                var line1 = $"Oda {odaNo} - {fiyat:C2}/gece";
                e.Graphics.DrawString(line1, boldFont, Brushes.Black, 
                    new Point(e.Bounds.X + 5, e.Bounds.Y + 3));

                // İkinci satır - Oda Tipi ve Özellikler (artık siyah renkte)
                var line2 = $"{odaTipi} - {ozellikler}";
                e.Graphics.DrawString(line2, normalFont, Brushes.Black, 
                    new Point(e.Bounds.X + 5, e.Bounds.Y + 22));
            }
        }
    }
}