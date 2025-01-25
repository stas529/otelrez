using System;  // Exception için
using MySql.Data.MySqlClient;
using System.Data;
using OtelRezervasyon.Models;
using System.Windows.Forms;  // MessageBox için

namespace OtelRezervasyon.DataAccess
{
    public class FaturaDAL
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();

        public DataTable GetAllFaturalar()
        {
            var dt = new DataTable();
            
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            f.faturaId,
                            CONCAT(m.ad, ' ', m.soyad) as musteriAdSoyad,
                            m.tcKimlik as musteriTC,
                            o.odaNumarasi as odaNo,
                            r.girisTarihi,
                            r.cikisTarihi,
                            f.toplamFiyat,
                            f.odemeTarihi,
                            f.odemeDurumu
                        FROM fatura f
                        JOIN rezervasyon r ON f.rezervasyonId = r.rezervasyonId
                        JOIN musteri m ON r.musteriId = m.musteriId
                        JOIN oda o ON r.odaId = o.odaId
                        ORDER BY f.odemeTarihi DESC";

                        using (var cmd = new MySqlCommand(sql, conn))
                        using (var adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Faturalar listelenirken hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }

        public void FaturaKes(int rezervasyonId, decimal toplamFiyat)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO Fatura 
                    (rezervasyonId, toplamFiyat, odemeTarihi, odemeDurumu) 
                    VALUES 
                    (@rezId, @fiyat, @tarih, 'BEKLEMEDE')";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@rezId", rezervasyonId);
                    cmd.Parameters.AddWithValue("@fiyat", toplamFiyat);
                    cmd.Parameters.AddWithValue("@tarih", System.DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool FaturaOde(int faturaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        UPDATE Fatura 
                        SET odemeDurumu = 'ODENDI',
                            odemeTarihi = NOW()
                        WHERE faturaId = @faturaId";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@faturaId", faturaId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatura ödenirken hata: " + ex.Message);
                return false;
            }
        }

        public bool FaturaIptal(int faturaId)
        {
            using (var conn = _db.GetConnection())
            {
                try
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        UPDATE fatura 
                        SET odemeDurumu = 'IPTAL',
                            iptalTarihi = NOW()
                        WHERE faturaId = @faturaId", conn);

                    cmd.Parameters.AddWithValue("@faturaId", faturaId);
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fatura iptal edilirken hata: " + ex.Message);
                    return false;
                }
            }
        }

        public Fatura GetFaturaById(int faturaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            f.faturaId,
                            f.rezervasyonId,
                            f.toplamFiyat,
                            f.odemeDurumu,
                            f.odemeTarihi,
                            r.girisTarihi,
                            r.cikisTarihi,
                            CONCAT(m.ad, ' ', m.soyad) as musteriAdSoyad,
                            m.tcKimlik as musteriTC,
                            o.odaNumarasi as odaNo,
                            o.temelFiyat as gunlukUcret
                        FROM fatura f
                        JOIN rezervasyon r ON f.rezervasyonId = r.rezervasyonId
                        JOIN musteri m ON r.musteriId = m.musteriId
                        JOIN oda o ON r.odaId = o.odaId
                        WHERE f.faturaId = @faturaId";

                        using (var cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@faturaId", faturaId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return new Fatura
                                    {
                                        faturaId = Convert.ToInt32(reader["faturaId"]),
                                        rezervasyonId = Convert.ToInt32(reader["rezervasyonId"]),
                                        toplamFiyat = Convert.ToDecimal(reader["toplamFiyat"]),
                                        odemeDurumu = reader["odemeDurumu"].ToString(),
                                        odemeTarihi = reader["odemeTarihi"] == DBNull.Value ? 
                                            DateTime.MinValue : Convert.ToDateTime(reader["odemeTarihi"]),
                                        musteriAdSoyad = reader["musteriAdSoyad"].ToString(),
                                        musteriTC = reader["musteriTC"].ToString(),
                                        odaNo = Convert.ToInt32(reader["odaNo"]),
                                        girisTarihi = Convert.ToDateTime(reader["girisTarihi"]),
                                        cikisTarihi = Convert.ToDateTime(reader["cikisTarihi"]),
                                        gunlukUcret = Convert.ToDecimal(reader["gunlukUcret"])
                                    };
                                }
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatura bilgileri alınırken hata: {ex.Message}");
            }
            return null;
        }

        public bool DeleteFatura(int faturaId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = "DELETE FROM fatura WHERE faturaId = @faturaId";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@faturaId", faturaId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatura silinirken hata: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool AddFatura(Fatura fatura)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        INSERT INTO fatura 
                        (rezervasyonId, toplamFiyat, odemeDurumu, odemeTarihi) 
                        VALUES 
                        (@rezervasyonId, @toplamFiyat, @odemeDurumu, @tarih)", conn);

                    cmd.Parameters.AddWithValue("@rezervasyonId", fatura.rezervasyonId);
                    cmd.Parameters.AddWithValue("@toplamFiyat", fatura.toplamFiyat);
                    cmd.Parameters.AddWithValue("@odemeDurumu", "BEKLEMEDE");
                    cmd.Parameters.AddWithValue("@tarih", DateTime.Now);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatura eklenirken hata: " + ex.Message);
                return false;
            }
        }
    }
} 