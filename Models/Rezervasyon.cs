using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtelRezervasyon.Models
{
    public class Rezervasyon
    {
        public int RezervasyonId { get; set; }
        public int MusteriId { get; set; }
        public int OdaId { get; set; }
        public DateTime GirisTarihi { get; set; }
        public DateTime CikisTarihi { get; set; }
        public decimal Fiyat { get; set; }
        public string Durum { get; set; }

        // Navigation property
        public Oda Oda { get; set; }

        // View properties
        public string MusteriTC { get; set; }
        public string MusteriAd { get; set; }
        public string MusteriSoyad { get; set; }
        public string MusteriTelefon { get; set; }

        public decimal ToplamFiyatHesapla(int gunSayisi)
        {
            if (Oda == null) return 0;

            decimal gunlukFiyat = Oda.FiyatHesapla();
            decimal sezonCarpani = 1.0m;  
            decimal promosyonIndirim = 0.0m;  

            return (gunlukFiyat * gunSayisi * sezonCarpani) * (1 - promosyonIndirim);
        }
    }
}
