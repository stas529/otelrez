namespace OtelRezervasyon.Models
{
    public class OdaListeItem
    {
        public int OdaId { get; set; }
        public int OdaNo { get; set; }
        public decimal Fiyat { get; set; }
        public string OdaTipi { get; set; }
        public string Ozellikler { get; set; }
    }
} 