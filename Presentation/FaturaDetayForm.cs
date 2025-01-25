using System;
using System.Windows.Forms;
using System.Drawing;
using OtelRezervasyon.Models;

namespace OtelRezervasyon.Presentation
{
    public partial class FaturaDetayForm : Form
    {
        private readonly Fatura _fatura;

        public FaturaDetayForm(Fatura fatura)
        {
            InitializeComponent();
            _fatura = fatura;
            LoadFaturaDetails();
        }

        private void LoadFaturaDetails()
        {
            var txtDetay = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ScrollBars = ScrollBars.Vertical
            };

            txtDetay.Text = $@"
═══════════════════════════════════
        OTEL ADI FATURA
═══════════════════════════════════

Fatura No: {_fatura.faturaId}
Tarih: {_fatura.odemeTarihi:dd/MM/yyyy HH:mm}

MUSTERI BILGILERI
────────────────────────
Ad Soyad: {_fatura.musteriAdSoyad}
TC Kimlik: {_fatura.musteriTC}

KONAKLAMA BILGILERI
────────────────────────
Oda No: {_fatura.odaNo}
Giris: {_fatura.girisTarihi:dd/MM/yyyy}
Cikis: {_fatura.cikisTarihi:dd/MM/yyyy}
Sure: {(_fatura.cikisTarihi - _fatura.girisTarihi).Days} gun

UCRET DETAYI
────────────────────────
Gunluk Ucret: {_fatura.gunlukUcret:C2}
Toplam Konaklama: {_fatura.toplamFiyat:C2}
────────────────────────
GENEL TOPLAM: {_fatura.toplamFiyat:C2}

Odeme Durumu: ODENDI

═══════════════════════════════════";

            this.Controls.Add(txtDetay);
        }
    }
}