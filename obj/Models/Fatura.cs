namespace OtelRezervasyon.Models
{
    public class Fatura
    {
        public int FaturaId { get; set; }
        public int RezervasyonId { get; set; }
        public decimal Tutar { get; set; }
        public string Durum { get; set; }
        public DateTime Tarih { get; set; }
    }
} 