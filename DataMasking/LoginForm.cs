using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DataMasking.Database;
using DataMasking.Network;
using DataMasking.Utils;

namespace DataMasking
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblStatus;
        private Panel pnlCard;

        // Theme colors
        private static readonly Color BgDark    = Color.FromArgb(15, 17, 28);
        private static readonly Color CardBg    = Color.FromArgb(25, 28, 42);
        private static readonly Color Accent    = Color.FromArgb(0, 180, 160);
        private static readonly Color AccentAlt = Color.FromArgb(99, 102, 241);
        private static readonly Color TextPrimary   = Color.FromArgb(230, 232, 250);
        private static readonly Color TextSecondary = Color.FromArgb(140, 145, 170);
        private static readonly Color InputBg   = Color.FromArgb(35, 38, 58);
        private static readonly Color InputBorder = Color.FromArgb(60, 64, 90);

        private Network.ClientService clientService;

        public bool IsAuthenticated { get; private set; }
        public Network.ServerResponse LoginResponse { get; private set; }
        public string LoggedInUsername { get; private set; }

        public LoginForm(Network.ClientService clientService)
        {
            this.clientService = clientService;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "DataMasking – Đăng nhập";
            this.Size = new Size(460, 560);
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;

            // ── Background gradient panel (full form)
            Panel pnlBg = new Panel { Dock = DockStyle.Fill };
            pnlBg.Paint += (s, e) =>
            {
                using var br = new LinearGradientBrush(pnlBg.ClientRectangle,
                    Color.FromArgb(15, 17, 28), Color.FromArgb(22, 24, 40),
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(br, pnlBg.ClientRectangle);
            };
            this.Controls.Add(pnlBg);

            // ── Close button (top-right)
            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                Location = new Point(this.Width - 40, 8),
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();
            pnlBg.Controls.Add(btnClose);

            // ── Drag support (click-drag on background)
            Point dragStart = Point.Empty;
            pnlBg.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            pnlBg.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    this.Location = new Point(this.Left + e.X - dragStart.X, this.Top + e.Y - dragStart.Y);
            };

            // ── Logo / icon area
            Label lblIcon = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI Emoji", 36),
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = TextPrimary
            };
            lblIcon.Location = new Point((this.Width - lblIcon.PreferredWidth) / 2, 30);
            pnlBg.Controls.Add(lblIcon);

            Label lblAppName = new Label
            {
                Text = "DataMasking",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Accent
            };
            lblAppName.Location = new Point((this.Width - lblAppName.PreferredWidth) / 2, 112);
            pnlBg.Controls.Add(lblAppName);

            Label lblSubtitle = new Label
            {
                Text = "Hệ thống bảo mật & che giấu dữ liệu",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = TextSecondary
            };
            lblSubtitle.Location = new Point((this.Width - lblSubtitle.PreferredWidth) / 2, 155);
            pnlBg.Controls.Add(lblSubtitle);

            // ── Card panel
            pnlCard = new Panel
            {
                Size = new Size(360, 300),
                Location = new Point((this.Width - 360) / 2, 190),
                BackColor = CardBg
            };
            pnlCard.Paint += (s, e) =>
            {
                // Rounded rect border
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(55, 60, 85), 1);
                using var path = RoundedRect(pnlCard.ClientRectangle, 12);
                e.Graphics.DrawPath(pen, path);
            };
            pnlBg.Controls.Add(pnlCard);

            int cx = 30;

            // Username label + field
            Label lblUsr = MakeLabel("Tên đăng nhập", cx, 22);
            pnlCard.Controls.Add(lblUsr);

            txtUsername = MakeInput(cx, 46, true);
            txtUsername.PlaceholderText = "Nhập username...";
            pnlCard.Controls.Add(txtUsername);

            // Password label + field
            Label lblPwd = MakeLabel("Mật khẩu", cx, 96);
            pnlCard.Controls.Add(lblPwd);

            txtPassword = MakeInput(cx, 120, true);
            txtPassword.PasswordChar = '●';
            txtPassword.PlaceholderText = "Nhập mật khẩu...";
            pnlCard.Controls.Add(txtPassword);

            // Status label
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(229, 62, 62),
                AutoSize = false,
                Size = new Size(300, 18),
                Location = new Point(cx, 166),
                BackColor = Color.Transparent
            };
            pnlCard.Controls.Add(lblStatus);

            // Login button
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(300, 42),
                Location = new Point(cx, 190),
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            // hover effect
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(0, 160, 142);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Accent;
            pnlCard.Controls.Add(btnLogin);

            // Version / footer
            Label lblFooter = new Label
            {
                Text = "v1.0 · RSA + AES Hybrid Encryption",
                Font = new Font("Segoe UI", 7.5f),
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(70, 75, 100)
            };
            lblFooter.Location = new Point((this.Width - lblFooter.PreferredWidth) / 2, 490);
            pnlBg.Controls.Add(lblFooter);

            this.AcceptButton = btnLogin;
        }

        // ── Helper factories
        private Label MakeLabel(string text, int x, int y) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Location = new Point(x, y),
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = TextSecondary
        };

        private TextBox MakeInput(int x, int y, bool wide) => new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Location = new Point(x, y),
            Size = new Size(300, 30),
            BackColor = InputBg,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ── Login logic
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                lblStatus.Text = "⚠  Vui lòng nhập đầy đủ thông tin.";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "ĐANG XÁC THỰC...";
            lblStatus.Text = "";

            try
            {
                ServerResponse response = await clientService.SendLoginRequestAsync(username, password);

                if (response.Success)
                {
                    IsAuthenticated = true;
                    LoginResponse = response;
                    LoggedInUsername = username;

                    // Open Dashboard, hide login form
                    var dashboard = new DashboardForm(username, response);
                    dashboard.OnLogout += () =>
                    {
                        this.Show();
                        txtPassword.Clear();
                        txtPassword.Focus();
                        lblStatus.Text = "";
                        txtUsername.Clear();
                    };
                    this.Hide();
                    dashboard.Show();
                }
                else
                {
                    lblStatus.Text = "✗  Sai tài khoản / mật khẩu hoặc Server chưa chạy.";
                    lblStatus.ForeColor = Color.FromArgb(229, 62, 62);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : "");
                bool serverDown = msg.Contains("refused") || msg.Contains("actively refused")
                               || msg.Contains("No connection") || msg.Contains("timed out")
                               || msg.Contains("target machine");

                if (serverDown)
                {
                    lblStatus.Text = "⚠  Server chưa được khởi động. Vui lòng Start Server trước!";
                    lblStatus.ForeColor = Color.FromArgb(236, 153, 75); // orange warning
                }
                else
                {
                    lblStatus.Text = "✗  Lỗi kết nối: " + ex.Message;
                    lblStatus.ForeColor = Color.FromArgb(229, 62, 62);
                }
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "ĐĂNG NHẬP";
            }
        }
    }
}
