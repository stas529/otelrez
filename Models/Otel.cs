using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtelRezervasyon.Models
{
    public class Otel
    {
        public int OtelId { get; set; }
        public string OtelAdi { get; set; }
        public int OdaSayisi { get; set; }
        public string Adres { get; set; }
        public string Telefon1 { get; set; }
        public string Telefon2 { get; set; }
    }
}