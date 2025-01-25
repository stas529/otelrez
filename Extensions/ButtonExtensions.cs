using System.Drawing;
using System.Windows.Forms;

namespace OtelRezervasyon.Extensions
{
    public static class ButtonExtensions
    {
        public static void ApplyCustomStyle(this Button btn, string type = "primary")
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.Height = 40;
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);

            switch (type.ToLower())
            {
                case "danger":
                    btn.BackColor = Color.FromArgb(220, 53, 69);      // Bootstrap danger - daha koyu kırmızı
                    btn.ForeColor = Color.White;
                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(200, 35, 51);
                    btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(220, 53, 69);
                    break;

                case "success":
                    btn.BackColor = Color.FromArgb(40, 167, 69);     // Bootstrap success
                    btn.ForeColor = Color.White;
                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(33, 136, 56);
                    btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(40, 167, 69);
                    break;

                case "warning":
                    btn.BackColor = Color.FromArgb(255, 193, 7);     // Bootstrap warning
                    btn.ForeColor = Color.Black;
                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(224, 168, 0);
                    btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(255, 193, 7);
                    break;

                case "info":
                    btn.BackColor = Color.FromArgb(23, 162, 184);    // Bootstrap info
                    btn.ForeColor = Color.White;
                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(19, 132, 150);
                    btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(23, 162, 184);
                    break;

                default: // primary
                    btn.BackColor = Color.FromArgb(0, 123, 255);     // Bootstrap primary
                    btn.ForeColor = Color.White;
                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(0, 105, 217);
                    btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(0, 123, 255);
                    break;
            }
        }
    }
} 