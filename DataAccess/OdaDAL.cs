using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using OtelRezervasyon.Models;
using System.Data.SqlClient;

namespace OtelRezervasyon.DataAccess
{
    // Otel odalarıyla ilgili veritabanı işlemlerini yapan sınıf
    public class OdaDAL
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();

        // Bütün odaları listeler
        public DataTable GetAllOdalar()
        {
            var dt = new DataTable();
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            o.odaId as OdaId,
                            o.odaNumarasi as OdaNumarasi,
                            o.temelFiyat as TemelFiyat,
                            o.durum as Durum,
                            CASE 
                                WHEN oz.odaId IS NOT NULL THEN 'Özel Oda'
                                ELSE 'Standart Oda'
                            END as OdaTipi,
                            CONCAT_WS(', ',
                                CASE WHEN oz.ciftYatakli = 1 THEN 'Çift Yatak' END,
                                CASE WHEN oz.genisBalkon = 1 THEN 'Geniş Balkon' END,
                                CASE WHEN oz.jakuzi = 1 THEN 'Jakuzi' END,
                                CASE WHEN oz.sehirManzara = 1 THEN 'Şehir Manzara' END
                            ) as Ozellikler
                        FROM oda o
                        LEFT JOIN ozeloda oz ON o.odaId = oz.odaId
                        ORDER BY o.odaNumarasi", conn);

                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);

                    if(dt.Columns.Count > 0)
                    {
                        dt.Columns["OdaId"].Caption = "Oda ID";
                        dt.Columns["OdaNumarasi"].Caption = "Oda No";
                        dt.Columns["TemelFiyat"].Caption = "Fiyat (₺)";
                        dt.Columns["Durum"].Caption = "Durum";
                        dt.Columns["OdaTipi"].Caption = "Oda Tipi";
                        dt.Columns["Ozellikler"].Caption = "Özellikler";
                    }

                    if(dt.Columns.Contains("TemelFiyat"))
                    {
                        dt.Columns["TemelFiyat"].DataType = typeof(decimal);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Odalar listelenirken hata: " + ex.Message);
            }
            return dt;
        }

        // Sadece boş odaları listeler
        public DataTable GetBosOdalar()
        {
            var dt = new DataTable();
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            o.odaId,
                            o.odaNumarasi,
                            o.temelFiyat,
                            o.durum,
                            CASE 
                                WHEN oz.odaId IS NOT NULL THEN 'Özel Oda'
                                ELSE 'Standart Oda'
                            END as odaTipi,
                            CONCAT_WS(', ',
                                CASE WHEN oz.ciftYatakli = 1 THEN 'Çift Yatak' END,
                                CASE WHEN oz.genisBalkon = 1 THEN 'Geniş Balkon' END,
                                CASE WHEN oz.jakuzi = 1 THEN 'Jakuzi' END,
                                CASE WHEN oz.sehirManzara = 1 THEN 'Şehir Manzara' END
                            ) as ozellikler
                        FROM oda o
                        LEFT JOIN ozeloda oz ON o.odaId = oz.odaId
                        WHERE o.durum = 'BOS'
                        ORDER BY o.odaNumarasi", conn);

                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Boş odalar listelenirken hata: " + ex.Message);
            }
            return dt;
        }

        // Verilen ID'ye sahip odanın fiyatını getirir
        public decimal GetOdaFiyat(int odaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT TemelFiyat FROM Oda WHERE OdaId = @odaId";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@odaId", odaId);
                        var result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oda fiyatı alınırken hata: " + ex.Message);
                return 0;
            }
        }

        // ID'ye göre oda bilgilerini getirir
        public Oda GetOdaById(int odaId)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT odaId, odaNumarasi, temelFiyat, durum
                        FROM oda 
                        WHERE odaId = @odaId", conn);

                    cmd.Parameters.AddWithValue("@odaId", odaId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Oda
                            {
                                OdaId = reader.GetInt32("odaId"),
                                OdaNumarasi = reader.GetInt32("odaNumarasi"),
                                TemelFiyat = reader.GetDecimal("temelFiyat"),
                                Durum = reader.GetString("durum")
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Oda bilgisi alınırken hata: " + ex.Message);
                }
                return null;
            }
        }

        // Yeni oda ekler
        public bool AddOda(Oda oda)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        INSERT INTO oda (odaNumarasi, temelFiyat, durum) 
                        VALUES (@odaNumarasi, @temelFiyat, @durum)", conn);

                    cmd.Parameters.AddWithValue("@odaNumarasi", oda.OdaNumarasi);
                    cmd.Parameters.AddWithValue("@temelFiyat", oda.TemelFiyat);
                    cmd.Parameters.AddWithValue("@durum", "BOS");

                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Oda eklenirken hata: " + ex.Message);
                    return false;
                }
            }
        }

        // Oda bilgilerini günceller
        public bool UpdateOda(Oda oda)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        UPDATE oda 
                        SET odaNumarasi = @odaNumarasi, 
                            temelFiyat = @temelFiyat, 
                            durum = @durum
                        WHERE odaId = @odaId", conn);

                    cmd.Parameters.AddWithValue("@odaId", oda.OdaId);
                    cmd.Parameters.AddWithValue("@odaNumarasi", oda.OdaNumarasi);
                    cmd.Parameters.AddWithValue("@temelFiyat", oda.TemelFiyat);
                    cmd.Parameters.AddWithValue("@durum", oda.Durum);

                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Oda güncellenirken hata: " + ex.Message);
                    return false;
                }
            }
        }

        // Odayı ve ilişkili kayıtları siler
        public bool DeleteOda(int odaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var checkCmd = new MySqlCommand(@"
                        SELECT 
                            COUNT(*) as ToplamRezervasyon,
                            SUM(CASE WHEN durum = 'AKTIF' THEN 1 ELSE 0 END) as AktifRezervasyon
                        FROM rezervasyon 
                        WHERE odaId = @odaId", conn);
                    
                    checkCmd.Parameters.AddWithValue("@odaId", odaId);
                    
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int toplamRezervasyon = Convert.ToInt32(reader["ToplamRezervasyon"]);
                            int aktifRezervasyon = Convert.ToInt32(reader["AktifRezervasyon"]);

                            if (aktifRezervasyon > 0)
                            {
                                MessageBox.Show(
                                    "Bu odada aktif rezervasyon bulunuyor!\n\n" +
                                    "Odayı silmek için önce mevcut rezervasyonun tamamlanmasını beklemelisiniz.",
                                    "Oda Silinemedi",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return false;
                            }

                            if (toplamRezervasyon > 0)
                            {
                                var result = MessageBox.Show(
                                    $"Bu odaya ait {toplamRezervasyon} adet geçmiş rezervasyon kaydı bulunuyor.\n\n" +
                                    "Odayı silmek için önce:\n" +
                                    "1. İlgili tüm rezervasyonlar silinecek\n" +
                                    "2. Bağlı faturalar silinecek\n\n" +
                                    "Bu işlem geri alınamaz! Devam etmek istiyor musunuz?",
                                    "Dikkat - Veri Kaybı",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning);

                                if (result == DialogResult.No)
                                    return false;

                                using (var transaction = conn.BeginTransaction())
                                {
                                    try
                                    {
                                        var cmdFatura = new MySqlCommand(@"
                                            DELETE FROM fatura 
                                            WHERE rezervasyonId IN (
                                                SELECT rezervasyonId 
                                                FROM rezervasyon 
                                                WHERE odaId = @odaId
                                            )", conn, transaction);
                                        cmdFatura.Parameters.AddWithValue("@odaId", odaId);
                                        cmdFatura.ExecuteNonQuery();

                                        var cmdRezervasyon = new MySqlCommand(
                                            "DELETE FROM rezervasyon WHERE odaId = @odaId", 
                                            conn, transaction);
                                        cmdRezervasyon.Parameters.AddWithValue("@odaId", odaId);
                                        cmdRezervasyon.ExecuteNonQuery();

                                        var cmdOda = new MySqlCommand(
                                            "DELETE FROM oda WHERE odaId = @odaId", 
                                            conn, transaction);
                                        cmdOda.Parameters.AddWithValue("@odaId", odaId);
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
                        }
                    }

                    var cmd = new MySqlCommand("DELETE FROM oda WHERE odaId = @odaId", conn);
                    cmd.Parameters.AddWithValue("@odaId", odaId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Odayı silerken bir sorun oluştu!\n\n" +
                    "Lütfen önce:\n" +
                    "1. Odada aktif rezervasyon olmadığından\n" +
                    "2. Tüm geçmiş kayıtların temizlendiğinden\n" +
                    "emin olun.",
                    "İşlem Başarısız",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        // Özel oda ekler
        public bool AddOzelOda(OzelOda oda)
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
                            var cmdOda = new MySqlCommand(@"
                                INSERT INTO oda (odaNumarasi, temelFiyat, durum) 
                                VALUES (@odaNumarasi, @temelFiyat, @durum);
                                SELECT LAST_INSERT_ID();", conn, transaction);

                            cmdOda.Parameters.AddWithValue("@odaNumarasi", oda.OdaNumarasi);
                            cmdOda.Parameters.AddWithValue("@temelFiyat", oda.TemelFiyat);
                            cmdOda.Parameters.AddWithValue("@durum", "BOS");

                            int odaId = Convert.ToInt32(cmdOda.ExecuteScalar());

                            var cmdOzelOda = new MySqlCommand(@"
                                INSERT INTO ozeloda (odaId, aciklama, ciftYatakli, genisBalkon, jakuzi, sehirManzara) 
                                VALUES (@odaId, @aciklama, @ciftYatakli, @genisBalkon, @jakuzi, @sehirManzara)", 
                                conn, transaction);

                            cmdOzelOda.Parameters.AddWithValue("@odaId", odaId);
                            cmdOzelOda.Parameters.AddWithValue("@aciklama", oda.Aciklama);
                            cmdOzelOda.Parameters.AddWithValue("@ciftYatakli", oda.CiftYatakli);
                            cmdOzelOda.Parameters.AddWithValue("@genisBalkon", oda.GenisBalkon);
                            cmdOzelOda.Parameters.AddWithValue("@jakuzi", oda.Jakuzi);
                            cmdOzelOda.Parameters.AddWithValue("@sehirManzara", oda.SehirManzara);

                            cmdOzelOda.ExecuteNonQuery();
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
                    MessageBox.Show("Özel oda eklenirken hata: " + ex.Message);
                    return false;
                }
            }
        }

        // ID'ye göre özel oda bilgilerini getirir
        public OzelOda GetOzelOdaById(int odaId)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT o.*, oz.aciklama, oz.ciftYatakli, oz.genisBalkon, oz.jakuzi, oz.sehirManzara
                        FROM oda o
                        JOIN ozeloda oz ON o.odaId = oz.odaId
                        WHERE o.odaId = @odaId", conn);

                    cmd.Parameters.AddWithValue("@odaId", odaId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new OzelOda
                            {
                                OdaId = reader.GetInt32("odaId"),
                                OdaNumarasi = reader.GetInt32("odaNumarasi"),
                                TemelFiyat = reader.GetDecimal("temelFiyat"),
                                Durum = reader.GetString("durum"),
                                Aciklama = reader.GetString("aciklama"),
                                CiftYatakli = reader.GetBoolean("ciftYatakli"),
                                GenisBalkon = reader.GetBoolean("genisBalkon"),
                                Jakuzi = reader.GetBoolean("jakuzi"),
                                SehirManzara = reader.GetBoolean("sehirManzara")
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Özel oda bilgisi alınırken hata: " + ex.Message);
                }
                return null;
            }
        }

        // Özel oda bilgilerini günceller
        public bool UpdateOzelOda(OzelOda oda)
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
                            var baseQuery = @"
                                UPDATE oda 
                                SET odaNumarasi = @odaNumarasi,
                                    temelFiyat = @temelFiyat,
                                    durum = @durum
                                WHERE odaId = @odaId";

                            using (var cmd = new MySqlCommand(baseQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@odaId", oda.OdaId);
                                cmd.Parameters.AddWithValue("@odaNumarasi", oda.OdaNumarasi);
                                cmd.Parameters.AddWithValue("@temelFiyat", oda.TemelFiyat);
                                cmd.Parameters.AddWithValue("@durum", oda.Durum);
                                cmd.ExecuteNonQuery();
                            }

                            var ozelQuery = @"
                                UPDATE ozeloda 
                                SET aciklama = @aciklama,
                                    ciftYatakli = @ciftYatakli,
                                    genisBalkon = @genisBalkon,
                                    jakuzi = @jakuzi,
                                    sehirManzara = @sehirManzara
                                WHERE odaId = @odaId";

                            using (var cmd = new MySqlCommand(ozelQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@odaId", oda.OdaId);
                                cmd.Parameters.AddWithValue("@aciklama", oda.Aciklama ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ciftYatakli", oda.CiftYatakli);
                                cmd.Parameters.AddWithValue("@genisBalkon", oda.GenisBalkon);
                                cmd.Parameters.AddWithValue("@jakuzi", oda.Jakuzi);
                                cmd.Parameters.AddWithValue("@sehirManzara", oda.SehirManzara);
                                cmd.ExecuteNonQuery();
                            }

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
                    MessageBox.Show($"Özel oda güncellenirken hata oluştu: {ex.Message}");
                    return false;
                }
            }
        }

        // Oda durumunu günceller
        public bool UpdateOdaDurum(int odaId, string yeniDurum)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        UPDATE oda 
                        SET durum = @durum
                        WHERE odaId = @odaId", conn);

                    cmd.Parameters.AddWithValue("@odaId", odaId);
                    cmd.Parameters.AddWithValue("@durum", yeniDurum);

                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Oda durumu güncellenirken hata: " + ex.Message);
                    return false;
                }
            }
        }

        // Veritabanından veri çeker
        private DataTable ExecuteQuery(string query)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(query, conn);
                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);

                    if(dt.Columns.Count > 0)
                    {
                        if(dt.Columns.Contains("odaId"))
                            dt.Columns["odaId"].Caption = "Oda ID";
                        
                        if(dt.Columns.Contains("odaNumarasi"))
                            dt.Columns["odaNumarasi"].Caption = "Oda No";
                        
                        if(dt.Columns.Contains("temelFiyat"))
                        {
                            dt.Columns["temelFiyat"].Caption = "Fiyat (₺)";
                            dt.Columns["temelFiyat"].DataType = typeof(decimal);
                        }
                        
                        if(dt.Columns.Contains("durum"))
                            dt.Columns["durum"].Caption = "Durum";
                        
                        if(dt.Columns.Contains("odaTipi"))
                            dt.Columns["odaTipi"].Caption = "Oda Tipi";
                        
                        if(dt.Columns.Contains("ozellikler"))
                            dt.Columns["ozellikler"].Caption = "Özellikler";
                        
                        if(dt.Columns.Contains("aciklama"))
                            dt.Columns["aciklama"].Caption = "Açıklama";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri çekilirken hata: " + ex.Message);
            }
            return dt;
        }
    }
}
