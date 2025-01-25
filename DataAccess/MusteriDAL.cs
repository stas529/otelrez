using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using OtelRezervasyon.Models;
using System.Data;
using System.Windows.Forms;

namespace OtelRezervasyon.DataAccess
{
    public class MusteriDAL
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();

        public bool AktifRezervasyonVarMi(int musteriId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT COUNT(*) FROM Rezervasyon 
                              WHERE MusteriId = @id AND Durum = 'AKTIF'";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", musteriId);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public void MusteriSil(int musteriId)
        {
            if (AktifRezervasyonVarMi(musteriId))
            {
                throw new Exception("Aktif rezervasyonu olan müşteri silinemez!");
            }

            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM Musteri WHERE MusteriId = @id";
                
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", musteriId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MusteriGuncelle(Musteri musteri)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string sql = @"UPDATE Musteri 
                              SET Telefon = @tel, 
                                  Email = @email, 
                                  Adres = @adres 
                              WHERE MusteriId = @id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tel", musteri.Telefon);
                    cmd.Parameters.AddWithValue("@email", musteri.Email);
                    cmd.Parameters.AddWithValue("@adres", musteri.Adres);
                    cmd.Parameters.AddWithValue("@id", musteri.MusteriId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void MusteriEkle(Musteri musteri)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO Musteri (TC, Ad, Soyad, Telefon) 
                              VALUES (@tc, @ad, @soyad, @tel)";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@tc", musteri.TcKimlik);
                    cmd.Parameters.AddWithValue("@ad", musteri.Ad);
                    cmd.Parameters.AddWithValue("@soyad", musteri.Soyad);
                    cmd.Parameters.AddWithValue("@tel", musteri.Telefon);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable GetAllMusteriler()
        {
            var dt = new DataTable();
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT 
                            musteriId,
                            tcKimlik,
                            ad,
                            soyad,
                            email,
                            telefon,
                            adres
                        FROM musteri", conn);
                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Müşteri verisi çekilirken hata: " + ex.Message);
            }
            return dt;
        }

        public int AddMusteri(Musteri musteri)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        INSERT INTO musteri 
                        (tcKimlik, ad, soyad, telefon, email, adres)
                        VALUES 
                        (@tcKimlik, @ad, @soyad, @telefon, @email, @adres);
                        SELECT LAST_INSERT_ID();", conn);

                    cmd.Parameters.AddWithValue("@tcKimlik", musteri.TcKimlik);
                    cmd.Parameters.AddWithValue("@ad", musteri.Ad);
                    cmd.Parameters.AddWithValue("@soyad", musteri.Soyad);
                    cmd.Parameters.AddWithValue("@telefon", musteri.Telefon);
                    cmd.Parameters.AddWithValue("@email", musteri.Email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adres", musteri.Adres ?? (object)DBNull.Value);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Müşteri eklenirken hata: " + ex.Message);
                return 0;
            }
        }

        public bool UpdateMusteri(Musteri musteri)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        UPDATE musteri 
                        SET tcKimlik=@tcKimlik, 
                            ad=@ad, 
                            soyad=@soyad, 
                            email=@email, 
                            telefon=@telefon, 
                            adres=@adres
                        WHERE musteriId=@musteriId", conn);

                    cmd.Parameters.AddWithValue("@musteriId", musteri.MusteriId);
                    cmd.Parameters.AddWithValue("@tcKimlik", musteri.TcKimlik);
                    cmd.Parameters.AddWithValue("@ad", musteri.Ad);
                    cmd.Parameters.AddWithValue("@soyad", musteri.Soyad);
                    cmd.Parameters.AddWithValue("@email", musteri.Email);
                    cmd.Parameters.AddWithValue("@telefon", musteri.Telefon);
                    cmd.Parameters.AddWithValue("@adres", musteri.Adres);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Müşteri güncellenirken hata: " + ex.Message);
                return false;
            }
        }

        public bool HasAktifRezervasyon(int musteriId)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
                        SELECT COUNT(*) 
                        FROM rezervasyon 
                        WHERE musteriId=@musteriId 
                        AND durum='AKTİF'", conn);

                    cmd.Parameters.AddWithValue("@musteriId", musteriId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rezervasyon kontrolü yapılırken hata: " + ex.Message);
                return false;
            }
        }

        public bool DeleteMusteri(int musteriId)
        {
            try
            {
                if (AktifRezervasyonVarMi(musteriId))
                {
                    MessageBox.Show("Aktif rezervasyonu olan müşteri silinemez!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    string sql = "DELETE FROM Musteri WHERE MusteriId = @id";
                    
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", musteriId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Müşteri silinirken hata: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public int AddOrUpdateMusteri(Musteri musteri)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    // Önce müşteriyi TC'ye göre ara
                    string checkSql = "SELECT musteriId FROM Musteri WHERE tcKimlik = @tc";
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tc", musteri.TcKimlik);
                        var existingId = cmd.ExecuteScalar();

                        if (existingId != null)
                        {
                            // Müşteri varsa güncelle
                            string updateSql = @"
                                UPDATE Musteri 
                                SET ad = @ad, soyad = @soyad, telefon = @telefon
                                WHERE musteriId = @id";

                            using (var updateCmd = new MySqlCommand(updateSql, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@id", Convert.ToInt32(existingId));
                                updateCmd.Parameters.AddWithValue("@ad", musteri.Ad);
                                updateCmd.Parameters.AddWithValue("@soyad", musteri.Soyad);
                                updateCmd.Parameters.AddWithValue("@telefon", musteri.Telefon);
                                updateCmd.ExecuteNonQuery();
                                return Convert.ToInt32(existingId);
                            }
                        }
                        else
                        {
                            // Müşteri yoksa ekle
                            string insertSql = @"
                                INSERT INTO Musteri (tcKimlik, ad, soyad, telefon)
                                VALUES (@tc, @ad, @soyad, @telefon);
                                SELECT LAST_INSERT_ID();";

                            using (var insertCmd = new MySqlCommand(insertSql, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@tc", musteri.TcKimlik);
                                insertCmd.Parameters.AddWithValue("@ad", musteri.Ad);
                                insertCmd.Parameters.AddWithValue("@soyad", musteri.Soyad);
                                insertCmd.Parameters.AddWithValue("@telefon", musteri.Telefon);
                                return Convert.ToInt32(insertCmd.ExecuteScalar());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Müşteri kaydedilirken hata: {ex.Message}");
                throw;
            }
        }
    }
}
