using System;

namespace OtelRezervasyon.Models
{
    public class Fatura
    {
        // Temel bilgiler
        public int faturaId { get; set; }
        public int rezervasyonId { get; set; }
        public decimal tutar { get; set; }
        public string durum { get; set; }
        public DateTime tarih { get; set; }
        
        // Müşteri bilgileri
        public int musteriId { get; set; }
        public int odaId { get; set; }
        public string musteriAdSoyad { get; set; }
        public string musteriTC { get; set; }
        
        // Oda ve tarih bilgileri
        public int odaNo { get; set; }
        public DateTime girisTarihi { get; set; }
        public DateTime cikisTarihi { get; set; }
        public DateTime odemeTarihi { get; set; }
        
        // Ücret bilgileri
        public decimal gunlukUcret { get; set; }
        public decimal toplamFiyat { get; set; }
        public decimal ekstraHizmetler { get; set; }
        
        // Durum bilgileri
        public string odemeDurumu { get; set; }  // "ODENDI", "BEKLIYOR"
        public string odemeTipi { get; set; }    // "NAKIT", "KREDI_KARTI", "HAVALE"
        
        // Hesaplama metodları
        public int KonaklamaGunSayisi => (cikisTarihi - girisTarihi).Days;
        
        public decimal ToplamTutar => toplamFiyat + ekstraHizmetler;
        
        public string DurumRengi => odemeDurumu == "ODENDI" ? "Green" : "Red";
        
        public string FormatliFiyat => $"{ToplamTutar:C2}";
        
        public string KisaOzet => $"Fatura #{faturaId} - {musteriAdSoyad} - {FormatliFiyat}";
    }
}