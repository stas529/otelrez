using System;
using System.Windows.Forms;
using System.Drawing;
using OtelRezervasyon.DataAccess;
using OtelRezervasyon.Presentation;

namespace OtelRezervasyon.Presentation
{
    public partial class AnaSayfa : Form
    {
        private Label lblHotelName;
        private Label lblSlogan;
        private Label lblAddress;
        private Label lblContact;
        private Button btnYonetici;
        private Panel panelInfo;
        private PictureBox pictureBox;
        private MySql.Data.MySqlClient.MySqlCommand mySqlCommand1;

        public AnaSayfa()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblHotelName = new System.Windows.Forms.Label();
            this.lblSlogan = new System.Windows.Forms.Label();
            this.lblAddress = new System.Windows.Forms.Label();
            this.lblContact = new System.Windows.Forms.Label();
            this.btnYonetici = new System.Windows.Forms.Button();
            this.panelInfo = new System.Windows.Forms.Panel();
            this.mySqlCommand1 = new MySql.Data.MySqlClient.MySqlCommand();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.panelInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // lblHotelName
            // 
            this.lblHotelName.AutoSize = true;
            this.lblHotelName.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold);
            this.lblHotelName.ForeColor = System.Drawing.Color.White;
            this.lblHotelName.Location = new System.Drawing.Point(250, 40);
            this.lblHotelName.Name = "lblHotelName";
            this.lblHotelName.Size = new System.Drawing.Size(350, 51);
            this.lblHotelName.TabIndex = 1;
            this.lblHotelName.Text = "BURSA TAŞ HOTEL";
            // 
            // lblSlogan
            // 
            this.lblSlogan.AutoSize = true;
            this.lblSlogan.Font = new System.Drawing.Font("Segoe UI Light", 16F);
            this.lblSlogan.ForeColor = System.Drawing.Color.White;
            this.lblSlogan.Location = new System.Drawing.Point(252, 90);
            this.lblSlogan.Name = "lblSlogan";
            this.lblSlogan.Size = new System.Drawing.Size(304, 30);
            this.lblSlogan.TabIndex = 2;
            this.lblSlogan.Text = "Bursa\'nın Tarihi Misafirperverliği";
            // 
            // lblAddress
            // 
            this.lblAddress.AutoSize = true;
            this.lblAddress.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblAddress.ForeColor = System.Drawing.Color.White;
            this.lblAddress.Location = new System.Drawing.Point(10, 10);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(274, 20);
            this.lblAddress.TabIndex = 0;
            this.lblAddress.Text = "Çekirge Caddesi No:5, Osmangazi/Bursa";
            // 
            // lblContact
            // 
            this.lblContact.AutoSize = true;
            this.lblContact.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.lblContact.ForeColor = System.Drawing.Color.White;
            this.lblContact.Location = new System.Drawing.Point(10, 40);
            this.lblContact.Name = "lblContact";
            this.lblContact.Size = new System.Drawing.Size(283, 20);
            this.lblContact.TabIndex = 1;
            this.lblContact.Text = "Tel: 0555-441-4141  |  Fax: 0555-441-4142";
            // 
            // btnYonetici
            // 
            this.btnYonetici.BackColor = System.Drawing.Color.Maroon;
            this.btnYonetici.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnYonetici.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnYonetici.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnYonetici.ForeColor = System.Drawing.Color.White;
            this.btnYonetici.Location = new System.Drawing.Point(250, 320);
            this.btnYonetici.Name = "btnYonetici";
            this.btnYonetici.Size = new System.Drawing.Size(200, 50);
            this.btnYonetici.TabIndex = 4;
            this.btnYonetici.Text = "Yönetici Girişi";
            this.btnYonetici.UseVisualStyleBackColor = false;
            this.btnYonetici.Click += new System.EventHandler(this.btnYonetici_Click);
            // 
            // panelInfo
            // 
            this.panelInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.panelInfo.Controls.Add(this.lblAddress);
            this.panelInfo.Controls.Add(this.lblContact);
            this.panelInfo.Location = new System.Drawing.Point(30, 200);
            this.panelInfo.Name = "panelInfo";
            this.panelInfo.Padding = new System.Windows.Forms.Padding(10);
            this.panelInfo.Size = new System.Drawing.Size(640, 100);
            this.panelInfo.TabIndex = 3;
            // 
            // mySqlCommand1
            // 
            this.mySqlCommand1.CacheAge = 0;
            this.mySqlCommand1.Connection = null;
            this.mySqlCommand1.EnableCaching = false;
            this.mySqlCommand1.Transaction = null;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.White;
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox.Image = global::OtelRezervasyon.Properties.Resources.otelLogo;
            this.pictureBox.Location = new System.Drawing.Point(30, 30);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(200, 150);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // AnaSayfa
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(700, 400);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.lblHotelName);
            this.Controls.Add(this.lblSlogan);
            this.Controls.Add(this.panelInfo);
            this.Controls.Add(this.btnYonetici);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "AnaSayfa";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bursa Taş Hotel - Rezervasyon Sistemi";
            this.panelInfo.ResumeLayout(false);
            this.panelInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void btnYonetici_Click(object sender, EventArgs e)
        {
            using (var loginForm = new YoneticiLoginForm())
            {
                this.Hide();
                
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    using (var yoneticiForm = new YoneticiForm())
                    {
                        yoneticiForm.ShowDialog();
                    }
                }
                
                this.Show();
            }
        }
    }
}