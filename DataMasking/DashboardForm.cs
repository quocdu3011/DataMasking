using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DataMasking.Network;

namespace DataMasking
{
    public class DashboardForm : Form
    {
        // Theme
        private static readonly Color BgDark      = Color.FromArgb(12, 14, 24);
        private static readonly Color SidebarBg   = Color.FromArgb(18, 20, 34);
        private static readonly Color Accent      = Color.FromArgb(0, 180, 160);
        private static readonly Color AccentAlt   = Color.FromArgb(99, 102, 241);
        private static readonly Color CardBg      = Color.FromArgb(22, 25, 40);
        private static readonly Color CardBorder  = Color.FromArgb(40, 44, 68);
        private static readonly Color TextPrimary = Color.FromArgb(225, 228, 250);
        private static readonly Color TextSecond  = Color.FromArgb(130, 136, 165);
        private static readonly Color TextMuted   = Color.FromArgb(70, 76, 100);
        private static readonly Color Danger      = Color.FromArgb(229, 62, 62);
        private static readonly Color Success     = Color.FromArgb(56, 189, 148);
        private static readonly Color Warn        = Color.FromArgb(236, 153, 75);

        private const int SidebarW = 220;
        private const int TopBarH  = 54;
        private const int Padding  = 24;

        public event Action OnLogout;

        private string username;
        private ServerResponse loginResponse;

        public DashboardForm(string username, ServerResponse loginResponse)
        {
            this.username = username;
            this.loginResponse = loginResponse;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text            = "DataMasking – Dashboard";
            this.Size            = new Size(1000, 660);
            this.MinimumSize     = new Size(860, 560);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor       = BgDark;
            this.ForeColor       = TextPrimary;

            BuildSidebar();
            BuildMainArea();
        }

        // ─────────────────────────────── Content width helpers
        // Must be called after the form is sized to get accurate values.
        private int ContentW => this.ClientSize.Width - SidebarW - Padding * 2 - SystemInformation.VerticalScrollBarWidth;

        // ══════════════════════════════════════════ SIDEBAR
        private void BuildSidebar()
        {
            Panel sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = SidebarW,
                BackColor = SidebarBg
            };
            sidebar.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(30, 34, 52), 1);
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };
            this.Controls.Add(sidebar);

            // ── App logo
            Label lblLogo = new Label
            {
                Text      = "🔐 DataMasking",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize  = false,
                Size      = new Size(190, 36),
                Location  = new Point(16, 20),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            sidebar.Controls.Add(lblLogo);

            Panel divTop = new Panel { Location = new Point(16, 62), Size = new Size(188, 1), BackColor = Color.FromArgb(30, 35, 55) };
            sidebar.Controls.Add(divTop);

            // ── Avatar circle
            Panel pnlAvatar = new Panel
            {
                Size      = new Size(64, 64),
                Location  = new Point((SidebarW - 64) / 2, 82),
                BackColor = Color.Transparent
            };
            pnlAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new LinearGradientBrush(pnlAvatar.ClientRectangle, Accent, AccentAlt, 135f);
                e.Graphics.FillEllipse(br, 0, 0, 63, 63);
                string initial = username.Length > 0 ? username[0].ToString().ToUpper() : "U";
                using var font = new Font("Segoe UI", 22, FontStyle.Bold);
                SizeF sz = e.Graphics.MeasureString(initial, font);
                e.Graphics.DrawString(initial, font, Brushes.White, (64 - sz.Width) / 2, (64 - sz.Height) / 2);
            };
            sidebar.Controls.Add(pnlAvatar);

            Label lblUser = new Label
            {
                Text      = "@" + username,
                Font      = new Font("Segoe UI Semibold", 10),
                ForeColor = TextPrimary,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            lblUser.Location = new Point((SidebarW - lblUser.PreferredWidth) / 2, 154);
            sidebar.Controls.Add(lblUser);

            Label lblRole = new Label
            {
                Text      = "● Đã xác thực",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Success,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            lblRole.Location = new Point((SidebarW - lblRole.PreferredWidth) / 2, 174);
            sidebar.Controls.Add(lblRole);

            Panel divMid = new Panel { Location = new Point(16, 205), Size = new Size(188, 1), BackColor = Color.FromArgb(30, 35, 55) };
            sidebar.Controls.Add(divMid);

            // ── Nav items
            int navY = 222;
            AddNavItem(sidebar, "🏠  Dashboard",       true,  ref navY);
            AddNavItem(sidebar, "👤  Hồ sơ cá nhân",  false, ref navY);
            AddNavItem(sidebar, "🔒  Bảo mật",         false, ref navY);
            AddNavItem(sidebar, "📊  Hoạt động",        false, ref navY);

            // ── Logout button (bottom-left)
            Button btnLogout = new Button
            {
                Text      = "⏻  Đăng xuất",
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Size      = new Size(188, 38),
                Location  = new Point(16, sidebar.Height - 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 30, 30),
                ForeColor = Danger,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnLogout.FlatAppearance.BorderColor = Color.FromArgb(80, 40, 40);
            btnLogout.FlatAppearance.BorderSize  = 1;
            btnLogout.Click      += (s, e) => { OnLogout?.Invoke(); this.Close(); };
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = Color.FromArgb(80, 25, 25);
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.FromArgb(50, 30, 30);
            sidebar.Controls.Add(btnLogout);
        }

        private void AddNavItem(Panel sidebar, string text, bool isActive, ref int y)
        {
            var btn = new Button
            {
                Text      = "  " + text,
                Font      = new Font("Segoe UI", 9, isActive ? FontStyle.Bold : FontStyle.Regular),
                Size      = new Size(188, 36),
                Location  = new Point(16, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = isActive ? Color.FromArgb(0, 90, 80) : Color.Transparent,
                ForeColor = isActive ? Accent : TextSecond,
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 0;
            sidebar.Controls.Add(btn);
            y += 42;
        }

        // ══════════════════════════════════════════ MAIN AREA
        private void BuildMainArea()
        {
            // Top bar
            Panel topBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = TopBarH,
                BackColor = Color.FromArgb(16, 18, 30)
            };
            topBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(28, 32, 50), 1);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };
            this.Controls.Add(topBar);
            topBar.BringToFront();

            Label lblPageTitle = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(24, 14)
            };
            topBar.Controls.Add(lblPageTitle);

            // Window buttons in topbar
            Button btnClose = MakeWindowButton("✕", Danger);
            btnClose.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.Location = new Point(topBar.Width - 36, 11);
            btnClose.Click   += (s, e) => Application.Exit();
            topBar.Controls.Add(btnClose);

            Button btnMin = MakeWindowButton("─", TextMuted);
            btnMin.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnMin.Location = new Point(topBar.Width - 72, 11);
            btnMin.Click   += (s, e) => this.WindowState = FormWindowState.Minimized;
            topBar.Controls.Add(btnMin);

            // Scrollable content area
            Panel content = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = BgDark,
                Padding    = new Padding(Padding, 20, Padding, 20)
            };
            this.Controls.Add(content);
            content.BringToFront();

            // Build content after layout is computed
            content.HandleCreated += (s, e) => BuildContent(content);
            this.Resize += (s, e) => ResizeContent(content);
        }

        // ══════════════════════════════════════════ SCROLLABLE CONTENT
        private Panel bannerPanel;
        private Panel[] statCards = new Panel[4];
        private Label lblInfoTitle;
        private Panel pnlInfo;
        private Panel noticePanel;

        private void BuildContent(Panel content)
        {
            if (content.InvokeRequired) { content.Invoke(new Action(() => BuildContent(content))); return; }

            content.Controls.Clear();

            // Use actual width; fallback to form width estimate
            int cw = content.ClientSize.Width - Padding * 2;
            if (cw <= 100) cw = this.Width - SidebarW - Padding * 2 - 20;

            var masked = loginResponse?.MaskedData;

            // ── Welcome banner
            bannerPanel = new Panel
            {
                Size     = new Size(cw, 88),
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };
            bannerPanel.Paint += PaintBanner;
            content.Controls.Add(bannerPanel);

            Label lblWelcome = new Label
            {
                Text      = $"👋  Xin chào, {username}!",
                Font      = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(20, 16)
            };
            bannerPanel.Controls.Add(lblWelcome);

            Label lblSub = new Label
            {
                Text      = "Đăng nhập thành công. Dữ liệu nhạy cảm đã được che giấu tự động theo chính sách bảo mật.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(200, 230, 220),
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(20, 52)
            };
            bannerPanel.Controls.Add(lblSub);

            // ── Stat cards row (4 equal cards)
            int cardGap = 12;
            int cardW   = (cw - cardGap * 3) / 4;
            int cardsY  = 104;

            string[] statLabels = { "🆔  Record ID",   "👤  Tài khoản",   "✅  Trạng thái",  "🕐  Đăng nhập"   };
            string[] statValues = { loginResponse?.RecordId.ToString() ?? "—", username, "Đã xác thực", DateTime.Now.ToString("HH:mm dd/MM") };
            Color[]  statColors = { Accent, AccentAlt, Success, Warn };

            for (int i = 0; i < 4; i++)
            {
                int col = i;
                statCards[i] = new Panel
                {
                    Size      = new Size(cardW, 86),
                    Location  = new Point(col * (cardW + cardGap), cardsY),
                    BackColor = CardBg
                };
                int capturedI = i;
                statCards[i].Paint += (s, e) => PaintStatCard((Panel)s, statColors[capturedI]);
                content.Controls.Add(statCards[i]);

                Label lv = new Label { Text = statValues[i], Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = statColors[i], AutoSize = true, BackColor = Color.Transparent, Location = new Point(12, 20) };
                Label ll = new Label { Text = statLabels[i], Font = new Font("Segoe UI", 8), ForeColor = TextSecond, AutoSize = true, BackColor = Color.Transparent, Location = new Point(12, 58) };
                statCards[i].Controls.Add(lv);
                statCards[i].Controls.Add(ll);
            }

            // ── Info section title
            lblInfoTitle = new Label
            {
                Text      = "Thông tin cá nhân  (dữ liệu đã được che giấu)",
                Font      = new Font("Segoe UI Semibold", 10),
                ForeColor = TextPrimary,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(0, 208)
            };
            content.Controls.Add(lblInfoTitle);

            // ── Info card
            pnlInfo = new Panel
            {
                Size      = new Size(cw, 268),
                Location  = new Point(0, 234),
                BackColor = CardBg
            };
            pnlInfo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen  = new Pen(CardBorder, 1);
                using var path = RoundedRect(new Rectangle(0, 0, pnlInfo.Width - 1, pnlInfo.Height - 1), 10);
                e.Graphics.DrawPath(pen, path);
            };
            content.Controls.Add(pnlInfo);

            string[,] fields =
            {
                { "👤  Họ và tên",      masked?.FullName    ?? "—" },
                { "📧  Email",          masked?.Email       ?? "—" },
                { "📱  Số điện thoại",  masked?.Phone       ?? "—" },
                { "💳  Thẻ tín dụng",  masked?.CreditCard  ?? "—" },
                { "🪪  Mã SSN",         masked?.SSN         ?? "—" },
                { "🏠  Địa chỉ",        masked?.Address     ?? "—" }
            };
            int rowY = 0;
            for (int i = 0; i < fields.GetLength(0); i++)
            {
                AddInfoRow(pnlInfo, fields[i, 0], fields[i, 1], rowY, i % 2 == 1);
                rowY += 44;
            }

            // ── Notice strip
            noticePanel = new Panel
            {
                Size      = new Size(cw, 46),
                Location  = new Point(0, 518),
                BackColor = Color.FromArgb(14, 50, 45)
            };
            noticePanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen  = new Pen(Color.FromArgb(0, 140, 120), 1);
                using var path = RoundedRect(new Rectangle(0, 0, noticePanel.Width - 1, noticePanel.Height - 1), 8);
                e.Graphics.DrawPath(pen, path);
            };
            content.Controls.Add(noticePanel);

            Label lblNote = new Label
            {
                Text      = "🔒  Dữ liệu nhạy cảm được che giấu tự động theo chính sách. Chỉ người có quyền mới xem được dữ liệu gốc.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Accent,
                AutoSize  = false,
                Size      = new Size(noticePanel.Width - 20, 40),
                Location  = new Point(10, 4),
                BackColor = Color.Transparent
            };
            noticePanel.Controls.Add(lblNote);

            // Total inner height
            content.AutoScrollMinSize = new Size(0, 580);
        }

        private void ResizeContent(Panel content)
        {
            if (bannerPanel == null) return;

            int cw = content.ClientSize.Width - Padding * 2;
            if (cw <= 100) return;

            int cardGap = 12;
            int cardW   = (cw - cardGap * 3) / 4;

            bannerPanel.Width  = cw;
            pnlInfo.Width      = cw;
            noticePanel.Width  = cw;

            for (int i = 0; i < 4; i++)
            {
                if (statCards[i] == null) continue;
                statCards[i].Width    = cardW;
                statCards[i].Location = new Point(i * (cardW + cardGap), statCards[i].Location.Y);
            }

            // Resize notice label too
            if (noticePanel.Controls.Count > 0)
                noticePanel.Controls[0].Size = new Size(noticePanel.Width - 20, 40);
        }

        // ─────────────────────── Paint helpers
        private void PaintBanner(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var br   = new LinearGradientBrush(p.ClientRectangle, Color.FromArgb(0, 100, 90), Color.FromArgb(40, 50, 100), LinearGradientMode.Horizontal);
            using var path = RoundedRect(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 12);
            e.Graphics.FillPath(br, path);
            using var pen  = new Pen(Color.FromArgb(80, 0, 180, 160), 1f);
            e.Graphics.DrawPath(pen, path);
        }

        private void PaintStatCard(Panel card, Color accentColor)
        {
            var e2 = new PaintEventArgs(card.CreateGraphics(), card.ClientRectangle);
            e2.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(CardBorder, 1);
            using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            e2.Graphics.DrawPath(pen, path);
            // Top accent strip
            using var br = new SolidBrush(accentColor);
            e2.Graphics.FillRectangle(br, new Rectangle(1, 0, card.Width - 2, 3));
            e2.Graphics.Dispose();
        }

        // ─────────────────────── Info row
        private void AddInfoRow(Panel parent, string label, string value, int y, bool shaded)
        {
            Panel row = new Panel
            {
                Size      = new Size(parent.Width, 44),
                Location  = new Point(0, y),
                BackColor = shaded ? Color.FromArgb(18, 21, 36) : Color.Transparent,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            parent.Controls.Add(row);

            Label lblKey = new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI Semibold", 9),
                ForeColor = TextSecond,
                AutoSize  = false,
                Size      = new Size(200, 44),
                Location  = new Point(16, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            row.Controls.Add(lblKey);

            Panel sep = new Panel { Size = new Size(1, 26), Location = new Point(216, 9), BackColor = CardBorder };
            row.Controls.Add(sep);

            Label lblVal = new Label
            {
                Text      = value,
                Font      = new Font("Cascadia Code", 9.5f),
                ForeColor = TextPrimary,
                AutoSize  = false,
                Size      = new Size(parent.Width - 240, 44),
                Location  = new Point(228, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            row.Controls.Add(lblVal);
        }

        // ─────────────────────── Window button
        private Button MakeWindowButton(string text, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Size      = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = fg,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 9)
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => b.BackColor = Color.FromArgb(40, 44, 60);
            b.MouseLeave += (s, e) => b.BackColor = Color.Transparent;
            return b;
        }

        // ─────────────────────── Rounded rect
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
    }
}
