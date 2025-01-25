// OzelOda.cs içindeki her şeyi sil
// Aşağıdaki kodu yapıştır:

using OtelRezervasyon.Models;

public class OzelOda : Oda
{
    public string Aciklama { get; set; }
    public bool CiftYatakli { get; set; }
    public bool GenisBalkon { get; set; }
    public bool Jakuzi { get; set; }
    public bool SehirManzara { get; set; }
    
    public override decimal FiyatHesapla()
    {
        decimal fiyat = TemelFiyat;
        
        if (CiftYatakli) fiyat *= 1.3m;     // +%30
        if (GenisBalkon) fiyat *= 1.15m;    // +%15
        if (Jakuzi) fiyat *= 1.2m;          // +%20
        if (SehirManzara) fiyat *= 1.1m;    // +%10
        
        return fiyat;
    }
}