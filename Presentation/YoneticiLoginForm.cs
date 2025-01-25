using System;
using System.Windows.Forms;
using OtelRezervasyon.DataAccess;
using MySql.Data.MySqlClient;

namespace OtelRezervasyon
{
    public partial class YoneticiLoginForm : Form
    {
        private readonly DatabaseConnection _db;

        public YoneticiLoginForm()
        {
            InitializeComponent();
            SetupForm();
            _db = new DatabaseConnection();
        }

        private void SetupForm()
        {
            this.Text = "Yönetici Girişi";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            txtSifre.PasswordChar = '*';
            txtSifre.UseSystemPasswordChar = true;

            this.AcceptButton = btnGiris;
        }

        private void btnGiris_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM yonetici WHERE kullaniciadi=@kadi AND sifre=@sifre", 
                        conn);

                    cmd.Parameters.AddWithValue("@kadi", txtKullanici.Text.Trim());
                    cmd.Parameters.AddWithValue("@sifre", txtSifre.Text);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Hatalı kullanıcı adı veya şifre!", 
                            "Giriş Başarısız", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                        
                        txtSifre.Clear();
                        txtSifre.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Giriş yapılırken bir hata oluştu: {ex.Message}", 
                    "Hata", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void txtSifre_TextChanged(object sender, EventArgs e) { }
        private void txtKullanici_TextChanged(object sender, EventArgs e) { }
    }
}
