using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtelRezervasyon.Models
{
    public class Oda  // internal yerine public yaptık - önemli!
    {
        public int OdaId { get; set; }
        public int OdaNumarasi { get; set; }
        public decimal TemelFiyat { get; set; }
        public string Durum { get; set; } = "BOŞ";

        public virtual decimal FiyatHesapla()
        {
            return TemelFiyat;
        }
    }
}