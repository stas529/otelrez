using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using OtelRezervasyon.Models;
using System.Windows.Forms;

namespace OtelRezervasyon.DataAccess
{
    public class RezervasyonDAL
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();
        private readonly MusteriDAL _musteriDAL = new MusteriDAL();

        // Event tanımı
        public event EventHandler RezervasyonTamamlandi;

        public DataTable GetAllRezervasyonlar()
        {
            var dt = new DataTable();
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            r.rezervasyonId as RezervasyonId,
                            r.odaId as OdaId,
                            CONCAT(m.ad, ' ', m.soyad) as musteriad,
                            m.tckimlik as tckimlik,
                            CONCAT(
                                SUBSTRING(m.telefon, 1, 4),
                                '-',
                                SUBSTRING(m.telefon, 5, 3),
                                '-',
                                SUBSTRING(m.telefon, 8, 4)
                            ) as telefon,
                            CONCAT('Oda ', o.odanumarasi) as odabilgisi,
                            o.odanumarasi as odano,
                            o.temelFiyat as fiyat,
                            r.giristarihi as giristarihi,
                            r.cikistarihi as cikistarihi,
                            r.fiyat as tutar,
                            r.durum as durum,
                            DATEDIFF(r.cikistarihi, r.giristarihi) as konaklamasuresi
                        FROM rezervasyon r
                        JOIN musteri m ON r.musteriid = m.musteriid
                        JOIN oda o ON r.odaid = o.odaid
                        ORDER BY r.giristarihi DESC", conn);
                    
                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);

                    // Kolon başlıklarını düzenle
                    if(dt.Columns.Count > 0)
                    {
                        dt.Columns["musteriad"].Caption = "Müşteri";
                        dt.Columns["tckimlik"].Caption = "TC Kimlik";
                        dt.Columns["telefon"].Caption = "Telefon";
                        dt.Columns["odabilgisi"].Caption = "Oda";
                        dt.Columns["fiyat"].Caption = "Günlük Ücret";
                        dt.Columns["giristarihi"].Caption = "Giriş Tarihi";
                        dt.Columns["cikistarihi"].Caption = "Çıkış Tarihi";
                        dt.Columns["tutar"].Caption = "Toplam Tutar";
                        dt.Columns["konaklamasuresi"].Caption = "Süre (Gün)";
                        dt.Columns["durum"].Caption = "Durum";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rezervasyon verisi çekilirken hata: " + ex.Message);
            }
            return dt;
        }

        public int AddRezervasyon(Rezervasyon rezervasyon)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        INSERT INTO rezervasyon 
                        (musteriId, odaId, girisTarihi, cikisTarihi, fiyat, durum)
                        VALUES 
                        (@musteriId, @odaId, @girisTarihi, @cikisTarihi, @fiyat, @durum);
                        SELECT LAST_INSERT_ID();", conn);

                    cmd.Parameters.AddWithValue("@musteriId", rezervasyon.MusteriId);
                    cmd.Parameters.AddWithValue("@odaId", rezervasyon.OdaId);
                    cmd.Parameters.AddWithValue("@girisTarihi", rezervasyon.GirisTarihi);
                    cmd.Parameters.AddWithValue("@cikisTarihi", rezervasyon.CikisTarihi);
                    cmd.Parameters.AddWithValue("@fiyat", rezervasyon.Fiyat);
                    cmd.Parameters.AddWithValue("@durum", rezervasyon.Durum);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rezervasyon eklenirken hata: " + ex.Message);
                return 0;
            }
        }

        public bool CheckIn(int rezervasyonId)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Rezervasyonu güncelle
                            var cmdRez = new MySqlCommand(@"
                                UPDATE rezervasyon 
                                SET durum = 'AKTIF', 
                                    girisTarihi = NOW()
                                WHERE rezervasyonId = @rezervasyonId", conn, transaction);
                            cmdRez.Parameters.AddWithValue("@rezervasyonId", rezervasyonId);
                            cmdRez.ExecuteNonQuery();

                            // Oda durumunu güncelle
                            var cmdOda = new MySqlCommand(@"
                                UPDATE oda 
                                SET durum = 'DOLU'
                                WHERE odaId = (
                                    SELECT odaId 
                                    FROM rezervasyon 
                                    WHERE rezervasyonId = @rezervasyonId
                                )", conn, transaction);
                            cmdOda.Parameters.AddWithValue("@rezervasyonId", rezervasyonId);
                            cmdOda.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Check-in işlemi başarısız: " + ex.Message);
                    return false;
                }
            }
        }

        public bool CheckOut(int rezervasyonId)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Rezervasyonu güncelle
                            var cmdRez = new MySqlCommand(@"
                                UPDATE rezervasyon 
                                SET durum = 'TAMAMLANDI', 
                                    cikisTarihi = NOW()
                                WHERE rezervasyonId = @rezervasyonId", conn, transaction);
                            cmdRez.Parameters.AddWithValue("@rezervasyonId", rezervasyonId);
                            cmdRez.ExecuteNonQuery();

                            // Oda durumunu güncelle
                            var cmdOda = new MySqlCommand(@"
                                UPDATE oda 
                                SET durum = 'TEMIZLIK'
                                WHERE odaId = (
                                    SELECT odaId 
                                    FROM rezervasyon 
                                    WHERE rezervasyonId = @rezervasyonId
                                )", conn, transaction);
                            cmdOda.Parameters.AddWithValue("@rezervasyonId", rezervasyonId);
                            cmdOda.ExecuteNonQuery();

                            // Fatura oluştur
                            var cmdFatura = new MySqlCommand(@"
                                INSERT INTO fatura (
                                    rezervasyonId, 
                                    tutar, 
                                    odemeDurumu, 
                                    olusturmaTarihi
                                )
                                SELECT 
                                    r.rezervasyonId,
                                    o.temelFiyat * DATEDIFF(NOW(), r.girisTarihi),
                                    'ODENMEDI',
                                    NOW()
                                FROM rezervasyon r
                                JOIN oda o ON r.odaId = o.odaId
                                WHERE r.rezervasyonId = @rezervasyonId", conn, transaction);
                            cmdFatura.Parameters.AddWithValue("@rezervasyonId", rezervasyonId);
                            cmdFatura.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Check-out işlemi başarısız: " + ex.Message);
                    return false;
                }
            }
        }

        public bool DeleteRezervasyon(int rezervasyonId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Önce faturayı sil
                            var cmdFatura = new MySqlCommand(
                                "DELETE FROM fatura WHERE rezervasyonId = @id", 
                                conn, transaction);
                            cmdFatura.Parameters.AddWithValue("@id", rezervasyonId);
                            cmdFatura.ExecuteNonQuery();

                            // Sonra rezervasyonu sil
                            var cmdRezervasyon = new MySqlCommand(
                                "DELETE FROM rezervasyon WHERE rezervasyonId = @id", 
                                conn, transaction);
                            cmdRezervasyon.Parameters.AddWithValue("@id", rezervasyonId);
                            cmdRezervasyon.ExecuteNonQuery();

                            // Odayı BOŞ durumuna getir
                            var cmdOda = new MySqlCommand(@"
                                UPDATE oda 
                                SET durum = 'BOŞ'
                                WHERE odaId = (
                                    SELECT odaId 
                                    FROM rezervasyon 
                                    WHERE rezervasyonId = @id
                                )", conn, transaction);
                            cmdOda.Parameters.AddWithValue("@id", rezervasyonId);
                            cmdOda.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Rezervasyon silinirken hata: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon silinirken hata: {ex.Message}");
                return false;
            }
        }

        public string FormatTelefonNo(string telefonNo)
        {
            MessageBox.Show($"FormatTelefonNo - Input: {telefonNo}"); // DEBUG 3
            
            // Sadece rakamları al
            telefonNo = new string(telefonNo.Where(char.IsDigit).ToArray());
            MessageBox.Show($"FormatTelefonNo - After Digits: {telefonNo}"); // DEBUG 4
            
            // 10 haneden uzunsa kes
            if (telefonNo.Length > 10)
                telefonNo = telefonNo.Substring(0, 10);
                
            // Format uygula
            if (telefonNo.Length == 10)
            {
                var result = $"{telefonNo.Substring(0, 4)}-{telefonNo.Substring(4, 3)}-{telefonNo.Substring(7, 3)}";
                MessageBox.Show($"FormatTelefonNo - Result: {result}"); // DEBUG 5
                return result;
            }
            
            MessageBox.Show($"FormatTelefonNo - No Format Applied: {telefonNo}"); // DEBUG 6
            return telefonNo;
        }

        public bool ValidateTelefonNo(string telefonNo)
        {
            // Sadece rakamları al
            telefonNo = new string(telefonNo.Where(char.IsDigit).ToArray());
            
            if (telefonNo.Length != 10) 
                return false;

            if (!telefonNo.StartsWith("05"))
                return false;

            int ucuncuHane = int.Parse(telefonNo[2].ToString());
            var gecerliOperatorKodlari = new[] { 3, 4, 5, 6 };
            
            if (!gecerliOperatorKodlari.Contains(ucuncuHane))
                return false;

            return true;
        }

        public Rezervasyon GetRezervasyonById(int id)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string sql = @"
                    SELECT 
                        r.*,
                        m.tcKimlik,
                        m.ad,
                        m.soyad,
                        m.telefon,
                        o.*
                    FROM rezervasyon r
                    JOIN musteri m ON r.musteriId = m.musteriId
                    JOIN oda o ON r.odaId = o.odaId
                    WHERE r.rezervasyonId = @id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            var rezervasyon = new Rezervasyon
                            {
                                RezervasyonId = dr.GetInt32("rezervasyonId"),
                                MusteriTC = dr.GetString("tcKimlik"),
                                MusteriAd = dr.GetString("ad"),
                                MusteriSoyad = dr.GetString("soyad"),
                                MusteriTelefon = dr.GetString("telefon"),
                                GirisTarihi = dr.GetDateTime("girisTarihi"),
                                CikisTarihi = dr.GetDateTime("cikisTarihi"),
                                OdaId = dr.GetInt32("odaId"),
                                Fiyat = dr.GetDecimal("temelFiyat"),
                                Oda = new Oda
                                {
                                    OdaId = dr.GetInt32("odaId"),
                                    OdaNumarasi = dr.GetInt32("odaNumarasi"),
                                    TemelFiyat = dr.GetDecimal("temelFiyat"),
                                    Durum = dr.GetString("durum")
                                }
                            };
                            return rezervasyon;
                        }
                    }
                }
            }
            return null;
        }

        public bool UpdateRezervasyon(Rezervasyon rez)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        UPDATE rezervasyon 
                        SET 
                            girisTarihi = @giris,
                            cikisTarihi = @cikis,
                            fiyat = @fiyat,
                            durum = @durum
                        WHERE rezervasyonId = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@giris", rez.GirisTarihi);
                        cmd.Parameters.AddWithValue("@cikis", rez.CikisTarihi);
                        cmd.Parameters.AddWithValue("@fiyat", rez.Fiyat);
                        cmd.Parameters.AddWithValue("@durum", rez.Durum);
                        cmd.Parameters.AddWithValue("@id", rez.RezervasyonId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon güncellenirken hata: {ex.Message}");
                return false;
            }
        }

        public int CreateRezervasyonWithFatura(Musteri musteri, Rezervasyon rezervasyon)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    
                    // 1. Müşteriyi ekle veya mevcut müşteriyi bul
                    int musteriId;
                    var cmdCheckMusteri = new MySqlCommand(
                        "SELECT musteriId FROM musteri WHERE tcKimlik = @tc", conn);
                    cmdCheckMusteri.Parameters.AddWithValue("@tc", musteri.TcKimlik);
                    
                    var existingMusteriId = cmdCheckMusteri.ExecuteScalar();
                    
                    if (existingMusteriId != null)
                    {
                        musteriId = Convert.ToInt32(existingMusteriId);
                    }
                    else
                    {
                        var cmdInsertMusteri = new MySqlCommand(@"
                            INSERT INTO musteri (tcKimlik, ad, soyad, telefon) 
                            VALUES (@tc, @ad, @soyad, @tel); 
                            SELECT LAST_INSERT_ID();", conn);
                        
                        cmdInsertMusteri.Parameters.AddWithValue("@tc", musteri.TcKimlik);
                        cmdInsertMusteri.Parameters.AddWithValue("@ad", musteri.Ad);
                        cmdInsertMusteri.Parameters.AddWithValue("@soyad", musteri.Soyad);
                        cmdInsertMusteri.Parameters.AddWithValue("@tel", musteri.Telefon);
                        
                        musteriId = Convert.ToInt32(cmdInsertMusteri.ExecuteScalar());
                    }

                    // 2. Rezervasyonu ekle
                    var cmdInsertRezervasyon = new MySqlCommand(@"
                        INSERT INTO rezervasyon (musteriId, odaId, girisTarihi, cikisTarihi, fiyat, durum)
                        VALUES (@musteriId, @odaId, @giris, @cikis, @fiyat, @durum);
                        SELECT LAST_INSERT_ID();", conn);

                    cmdInsertRezervasyon.Parameters.AddWithValue("@musteriId", musteriId);
                    cmdInsertRezervasyon.Parameters.AddWithValue("@odaId", rezervasyon.OdaId);
                    cmdInsertRezervasyon.Parameters.AddWithValue("@giris", rezervasyon.GirisTarihi);
                    cmdInsertRezervasyon.Parameters.AddWithValue("@cikis", rezervasyon.CikisTarihi);
                    cmdInsertRezervasyon.Parameters.AddWithValue("@fiyat", rezervasyon.Fiyat);
                    cmdInsertRezervasyon.Parameters.AddWithValue("@durum", rezervasyon.Durum);

                    int rezervasyonId = Convert.ToInt32(cmdInsertRezervasyon.ExecuteScalar());

                    return rezervasyonId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rezervasyon oluşturulurken hata: {ex.Message}");
                return -1;
            }
        }

        public DataTable GetRezervasyonlarByOdaId(int odaId)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            r.rezervasyonId,
                            CONCAT(m.ad, ' ', m.soyad) as musteriad,
                            m.tckimlik,
                            m.telefon,
                            o.odanumarasi,
                            r.giristarihi,
                            r.cikistarihi,
                            r.fiyat as tutar,
                            r.durum,
                            DATEDIFF(r.cikistarihi, r.giristarihi) as konaklamasuresi
                        FROM rezervasyon r
                        JOIN musteri m ON r.musteriid = m.musteriid
                        JOIN oda o ON r.odaid = o.odaid
                        WHERE r.odaid = @odaId
                        ORDER BY r.giristarihi DESC", conn);

                    cmd.Parameters.AddWithValue("@odaId", odaId);
                    
                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);

                    // Kolon başlıklarını düzenle
                    if(dt.Columns.Count > 0)
                    {
                        dt.Columns["musteriad"].Caption = "Müşteri";
                        dt.Columns["tckimlik"].Caption = "TC Kimlik";
                        dt.Columns["telefon"].Caption = "Telefon";
                        dt.Columns["odanumarasi"].Caption = "Oda No";
                        dt.Columns["giristarihi"].Caption = "Giriş Tarihi";
                        dt.Columns["cikistarihi"].Caption = "Çıkış Tarihi";
                        dt.Columns["tutar"].Caption = "Tutar";
                        dt.Columns["durum"].Caption = "Durum";
                        dt.Columns["konaklamasuresi"].Caption = "Süre (Gün)";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oda rezervasyonları çekilirken hata: " + ex.Message);
            }
            return dt;
        }

        public bool OdaHasAnyReservations(int odaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT COUNT(*) 
                        FROM rezervasyon 
                        WHERE odaId = @odaId", conn);

                    cmd.Parameters.AddWithValue("@odaId", odaId);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Oda rezervasyon kontrolü sırasında hata: {ex.Message}");
                return true;
            }
        }

        public bool DeleteOdaWithReservations(int odaId)
        {
            try
            {
                // Önce aktif rezervasyon kontrolü yapalım
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var checkCmd = new MySqlCommand(@"
                        SELECT COUNT(*) 
                        FROM rezervasyon 
                        WHERE odaId = @odaId 
                        AND durum = 'AKTIF'", conn);
                    
                    checkCmd.Parameters.AddWithValue("@odaId", odaId);
                    int aktifRezervasyonSayisi = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (aktifRezervasyonSayisi > 0)
                    {
                        MessageBox.Show(
                            "Bu oda şu anda kullanımda ve aktif rezervasyonları bulunuyor.\n\n" +
                            "Odayı silmek için önce mevcut rezervasyonların tamamlanmasını beklemelisiniz.",
                            "Oda Silinemedi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return false;
                    }

                    // Geçmiş rezervasyon kontrolü
                    var historyCmd = new MySqlCommand(@"
                        SELECT COUNT(*) 
                        FROM rezervasyon 
                        WHERE odaId = @odaId", conn);
                    
                    historyCmd.Parameters.AddWithValue("@odaId", odaId);
                    int toplamRezervasyonSayisi = Convert.ToInt32(historyCmd.ExecuteScalar());

                    if (toplamRezervasyonSayisi > 0)
                    {
                        var result = MessageBox.Show(
                            $"Bu odaya ait {toplamRezervasyonSayisi} adet geçmiş rezervasyon kaydı bulunuyor.\n\n" +
                            "Odayı silmek, tüm geçmiş rezervasyon kayıtlarını ve faturaları da silecektir.\n\n" +
                            "Devam etmek istiyor musunuz?",
                            "Dikkat - Veri Kaybı",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.No)
                        {
                            return false;
                        }
                    }

                    // Silme işlemine devam
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Önce faturalar silinir
                            var cmdFatura = new MySqlCommand(@"
                                DELETE FROM fatura 
                                WHERE rezervasyonId IN (
                                    SELECT rezervasyonId 
                                    FROM rezervasyon 
                                    WHERE odaId = @odaId
                                )", conn, transaction);
                            cmdFatura.Parameters.AddWithValue("@odaId", odaId);
                            cmdFatura.ExecuteNonQuery();

                            // Sonra rezervasyonlar silinir
                            var cmdRezervasyon = new MySqlCommand(@"
                                DELETE FROM rezervasyon 
                                WHERE odaId = @odaId", conn, transaction);
                            cmdRezervasyon.Parameters.AddWithValue("@odaId", odaId);
                            cmdRezervasyon.ExecuteNonQuery();

                            // En son oda silinir
                            var cmdOda = new MySqlCommand(@"
                                DELETE FROM oda 
                                WHERE odaId = @odaId", conn, transaction);
                            cmdOda.Parameters.AddWithValue("@odaId", odaId);
                            cmdOda.ExecuteNonQuery();

                            transaction.Commit();
                            MessageBox.Show(
                                "Oda ve ilişkili tüm kayıtlar başarıyla silindi.",
                                "İşlem Başarılı",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            MessageBox.Show(
                                "Odayı silerken bir sorun oluştu.\n\n" +
                                "Lütfen daha sonra tekrar deneyin veya sistem yöneticinize başvurun.",
                                "İşlem Başarısız",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show(
                    "Beklenmeyen bir hata oluştu.\n\n" +
                    "Lütfen tüm açık rezervasyonları kontrol edip tekrar deneyin.",
                    "Sistem Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        public string GetOdaRezervasyonDurumu(int odaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            COUNT(*) as ToplamRezervasyon,
                            SUM(CASE WHEN durum = 'AKTIF' THEN 1 ELSE 0 END) as AktifRezervasyon,
                            SUM(CASE WHEN durum = 'TAMAMLANDI' THEN 1 ELSE 0 END) as TamamlananRezervasyon
                        FROM rezervasyon 
                        WHERE odaId = @odaId", conn);

                    cmd.Parameters.AddWithValue("@odaId", odaId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int toplam = reader.GetInt32("ToplamRezervasyon");
                            int aktif = reader.GetInt32("AktifRezervasyon");
                            int tamamlanan = reader.GetInt32("TamamlananRezervasyon");

                            if (toplam == 0)
                                return "Bu odaya ait hiç rezervasyon bulunmamaktadır.";
                            
                            return $"Bu odaya ait {toplam} rezervasyon bulunuyor:\n" +
                                   $"- {aktif} aktif rezervasyon\n" +
                                   $"- {tamamlanan} tamamlanmış rezervasyon\n\n" +
                                   "Aktif rezervasyonu olan odalar silinemez.";
                        }
                        return "Rezervasyon bilgisi alınamadı.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Hata: {ex.Message}";
            }
        }
    }
}
