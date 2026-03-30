using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DataMasking.Database;
using DataMasking.Masking;
using DataMasking.Key;
using DataMasking.Network;
using DataMasking.Utils;

namespace DataMasking
{
    public partial class Main : Form
    {
        private DatabaseManager dbManager;
        private MaskingService maskingService;
        private RSAKeyPair serverKeyPair;  // Server's RSA key pair
        private ClientService clientService;
        private ServerService serverService;
        private bool isServerRunning = false;
        private Form serverLogForm;
        private TextBox txtServerLog;

        // Controls
        private TabControl tabControl;
        private DataGridView dgvOriginal, dgvEncrypted;
        private TextBox txtName, txtEmail, txtPhone, txtCard, txtSSN, txtAddress;
        private TextBox txtTransmissionLog;
        private ComboBox cboEncryptionType, cboMaskingType, cboMaskingTypeView;
        private Button btnAdd, btnMask, btnEncrypt, btnRefresh, btnDelete, btnInitDB, btnAddSample;
        private Button btnStartServer, btnStopServer;
        private Label lblStatus, lblServerStatus;
        private TextBox txtConnectionString;
        private ProgressBar progressServer;
        
        // User authentication controls
        private TextBox txtUsername, txtPassword;
        private Button btnCreateUser;

        public Main()
        {
            InitializeComponent();

            maskingService = new MaskingService();

            // Khởi tạo database manager với connection string mặc định
            dbManager = new DatabaseManager("36.50.54.109", "datamasking_db", "anonymous", "Thuanld@255", 3306);

            // Tạo RSA key pair cho server TRƯỚC KHI khởi tạo UI
            serverKeyPair = new RSAKeyPair(1024);

            // Khởi tạo Client và Server services
            clientService = new ClientService(serverKeyPair, "127.0.0.1", 8888);  // Client kết nối đến localhost:8888
            serverService = new ServerService(serverKeyPair, dbManager);  // Server có cả private key

            // Khởi tạo UI sau khi có serverKeyPair
            InitializeCustomComponents();

            // Subscribe to CLIENT transmission logger only
            TransmissionLogger.OnClientLog += LogTransmission;
            
            // Hiển thị form đăng nhập cho client
            ShowLoginForm();
        }
        
        private void ShowLoginForm()
        {
            // Login form nay không nhận dbManager truyền trực tiếp mà sẽ nhận ClientService
            // để trao đổi qua TCP với Server.
            LoginForm loginForm = new LoginForm(clientService);
            
            // Dùng Show() thay vì ShowDialog() để form Login có thể hiển thị song song với Main admin.
            loginForm.Show();
        }

        private void LogTransmission(string message)
        {
            if (txtTransmissionLog.InvokeRequired)
            {
                txtTransmissionLog.Invoke(new Action<string>(LogTransmission), message);
            }
            else
            {
                txtTransmissionLog.AppendText(message + Environment.NewLine);
            }
        }

        private void LogServerMessage(string message)
        {
            if (txtServerLog == null) return;
            if (txtServerLog.InvokeRequired)
            {
                try { txtServerLog.Invoke(new Action<string>(LogServerMessage), message); } catch { }
            }
            else
            {
                txtServerLog.AppendText(message + Environment.NewLine);
            }
        }

        // ===== MODERN THEME COLORS =====
        private static readonly Color ThemeBg = Color.FromArgb(30, 30, 46);           // Deep dark background
        private static readonly Color ThemePanel = Color.FromArgb(40, 42, 58);         // Panel/card background
        private static readonly Color ThemeHeader = Color.FromArgb(24, 24, 37);        // Header strip
        private static readonly Color ThemeAccent = Color.FromArgb(0, 150, 136);       // Teal accent
        private static readonly Color ThemeAccentHover = Color.FromArgb(0, 180, 160);  // Lighter teal
        private static readonly Color ThemeDanger = Color.FromArgb(239, 83, 80);       // Coral red
        private static readonly Color ThemeWarn = Color.FromArgb(255, 183, 77);        // Amber
        private static readonly Color ThemeSuccess = Color.FromArgb(102, 187, 106);    // Soft green
        private static readonly Color ThemeTextPrimary = Color.FromArgb(230, 230, 240); // Light text
        private static readonly Color ThemeTextSecondary = Color.FromArgb(160, 165, 185); // Muted text
        private static readonly Color ThemeTextMuted = Color.FromArgb(100, 105, 120);  // Very muted
        private static readonly Color ThemeInput = Color.FromArgb(50, 52, 70);         // Input background
        private static readonly Color ThemeInputBorder = Color.FromArgb(70, 75, 95);   // Input border
        private static readonly Color ThemeCyan = Color.FromArgb(0, 230, 230);         // Cyan for logs
        private static readonly Color ThemeTabBg = Color.FromArgb(35, 37, 52);         // Tab background

        private Button CreateModernButton(string text, Color bgColor, Color fgColor, int width = 130, int height = 34)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = bgColor,
                ForeColor = fgColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(bgColor.R + 20, 255),
                Math.Min(bgColor.G + 20, 255),
                Math.Min(bgColor.B + 20, 255));
            return btn;
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = ThemePanel;
            dgv.GridColor = ThemeInputBorder;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.DefaultCellStyle.BackColor = ThemePanel;
            dgv.DefaultCellStyle.ForeColor = ThemeTextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = ThemeAccent;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgv.DefaultCellStyle.Padding = new Padding(5, 3, 5, 3);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = ThemeHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ThemeAccentHover;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 48, 65);
            dgv.RowTemplate.Height = 30;
        }

        private void StyleTabPage(TabPage tab)
        {
            tab.BackColor = ThemePanel;
            tab.ForeColor = ThemeTextPrimary;
        }

        private void InitializeCustomComponents()
        {
            this.Text = "🔒 Data Masking System — Client-Server TCP Network";
            this.Size = new Size(1550, 870);
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = ThemeBg;
            this.ForeColor = ThemeTextPrimary;
            this.Font = new Font("Segoe UI", 9);

            // ═══════════════════════════════════════════
            // HEADER PANEL
            // ═══════════════════════════════════════════
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = ThemeHeader
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "🔒  DATA MASKING SYSTEM",
                Location = new Point(20, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Client-Server • TCP Network • RSA + AES Encryption",
                Location = new Point(310, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = ThemeTextMuted
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // ═══════════════════════════════════════════
            // CONNECTION & SERVER CONTROL PANEL
            // ═══════════════════════════════════════════
            Panel pnlControls = new Panel
            {
                Location = new Point(0, 50),
                Size = new Size(1500, 45),
                BackColor = Color.FromArgb(35, 37, 52)
            };
            this.Controls.Add(pnlControls);

            // Connection
            Label lblConn = new Label
            {
                Text = "🗄️ Connection:",
                Location = new Point(15, 12),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeTextSecondary
            };
            pnlControls.Controls.Add(lblConn);

            txtConnectionString = new TextBox
            {
                Location = new Point(135, 9),
                Size = new Size(400, 28),
                Text = "Server=36.50.54.109;Port=3306;Database=datamasking_db;Uid=anonymous;Pwd=1;",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8.5f)
            };
            pnlControls.Controls.Add(txtConnectionString);

            btnInitDB = CreateModernButton("⚡ Khởi tạo DB", ThemeAccent, Color.White, 115, 28);
            btnInitDB.Location = new Point(535, 8);
            btnInitDB.Click += BtnInitDB_Click;
            pnlControls.Controls.Add(btnInitDB);

            btnAddSample = CreateModernButton("＋ Dữ liệu mẫu", Color.FromArgb(70, 73, 95), ThemeTextPrimary, 125, 28);
            btnAddSample.Location = new Point(658, 8);
            btnAddSample.Click += BtnAddSample_Click;
            pnlControls.Controls.Add(btnAddSample);

            // Separator
            Label sep = new Label
            {
                Text = "│",
                Location = new Point(795, 8),
                AutoSize = true,
                ForeColor = ThemeTextMuted,
                Font = new Font("Segoe UI", 12)
            };
            pnlControls.Controls.Add(sep);

            // Server Controls
            btnStartServer = CreateModernButton("▶  START", ThemeSuccess, Color.White, 100, 28);
            btnStartServer.Location = new Point(820, 8);
            btnStartServer.Click += BtnStartServer_Click;
            pnlControls.Controls.Add(btnStartServer);

            btnStopServer = CreateModernButton("⏹  STOP", ThemeDanger, Color.White, 100, 28);
            btnStopServer.Location = new Point(928, 8);
            btnStopServer.Enabled = false;
            btnStopServer.Click += BtnStopServer_Click;
            pnlControls.Controls.Add(btnStopServer);

            lblServerStatus = new Label
            {
                Text = "●  STOPPED",
                Location = new Point(1040, 12),
                AutoSize = true,
                ForeColor = ThemeDanger,
                Font = new Font("Segoe UI Semibold", 9)
            };
            pnlControls.Controls.Add(lblServerStatus);

            // ── Admin: Login Masking Config
            Label lblLoginMask = new Label
            {
                Text = "🎭 Login Masking:",
                Location = new Point(1160, 12),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeWarn
            };
            pnlControls.Controls.Add(lblLoginMask);

            ComboBox cboLoginMasking = new ComboBox
            {
                Location = new Point(1300, 8),
                Size = new Size(195, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = ThemeInput,
                ForeColor = ThemeWarn
            };
            cboLoginMasking.Items.AddRange(new object[]
            {
                "Che mặt nạ ký tự",
                "Xáo trộn dữ liệu",
                "Thay thế bằng dữ liệu giả",
                "Thêm nhiễu vào ký tự số"
            });
            cboLoginMasking.SelectedIndex = 0;
            cboLoginMasking.SelectedIndexChanged += (s, e) =>
            {
                ServerService.LoginMaskingType = (Masking.MaskingType)cboLoginMasking.SelectedIndex;
            };
            pnlControls.Controls.Add(cboLoginMasking);

            // Progress Bar (hidden by default)
            progressServer = new ProgressBar
            {
                Location = new Point(1160, 12),
                Size = new Size(150, 20),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false
            };
            pnlControls.Controls.Add(progressServer);

            Label lblProgress = new Label
            {
                Name = "lblProgress",
                Text = "",
                Location = new Point(1320, 12),
                Size = new Size(160, 20),
                ForeColor = ThemeWarn,
                Font = new Font("Segoe UI", 8, FontStyle.Italic)
            };
            pnlControls.Controls.Add(lblProgress);

            // ═══════════════════════════════════════════
            // TAB CONTROL (LEFT SIDE)
            // ═══════════════════════════════════════════
            tabControl = new TabControl
            {
                Location = new Point(15, 105),
                Size = new Size(920, 600),
                Font = new Font("Segoe UI Semibold", 9.5f)
            };

            TabPage tabInput = new TabPage("  📤 Client — Nhập & Gửi  ");
            StyleTabPage(tabInput);
            InitializeInputTab(tabInput);
            tabControl.TabPages.Add(tabInput);

            TabPage tabOriginal = new TabPage("  📁 Dữ liệu Gốc  ");
            StyleTabPage(tabOriginal);
            InitializeOriginalTab(tabOriginal);
            tabControl.TabPages.Add(tabOriginal);

            TabPage tabMasked = new TabPage("  🎭 Dữ liệu Masked  ");
            StyleTabPage(tabMasked);
            InitializeMaskedTab(tabMasked);
            tabControl.TabPages.Add(tabMasked);

            TabPage tabEncrypted = new TabPage("  🔐 Mã hóa  ");
            StyleTabPage(tabEncrypted);
            InitializeEncryptedTab(tabEncrypted);
            tabControl.TabPages.Add(tabEncrypted);

            TabPage tabKeys = new TabPage("  🔑 Keys  ");
            StyleTabPage(tabKeys);
            InitializeKeysTab(tabKeys);
            tabControl.TabPages.Add(tabKeys);

            this.Controls.Add(tabControl);

            // ═══════════════════════════════════════════
            // TRANSMISSION LOG (RIGHT SIDE)
            // ═══════════════════════════════════════════
            Panel pnlLogHeader = new Panel
            {
                Location = new Point(945, 105),
                Size = new Size(535, 32),
                BackColor = ThemeHeader
            };
            this.Controls.Add(pnlLogHeader);

            Label lblLog = new Label
            {
                Text = "📡  CLIENT TRANSMISSION LOG",
                Location = new Point(10, 6),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeCyan
            };
            pnlLogHeader.Controls.Add(lblLog);

            Button btnClearLog = CreateModernButton("Xóa", Color.FromArgb(60, 63, 80), ThemeTextSecondary, 55, 24);
            btnClearLog.Location = new Point(470, 4);
            btnClearLog.Font = new Font("Segoe UI", 8);
            btnClearLog.Click += (s, e) => txtTransmissionLog.Clear();
            pnlLogHeader.Controls.Add(btnClearLog);

            txtTransmissionLog = new TextBox
            {
                Location = new Point(945, 137),
                Size = new Size(535, 568),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 15, 25),
                ForeColor = ThemeCyan,
                Font = new Font("Cascadia Code", 8.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                WordWrap = false
            };
            this.Controls.Add(txtTransmissionLog);

            // ═══════════════════════════════════════════
            // STATUS BAR (BOTTOM)
            // ═══════════════════════════════════════════
            Panel pnlStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = ThemeHeader
            };
            this.Controls.Add(pnlStatus);

            lblStatus = new Label
            {
                Text = "✓  Sẵn sàng — Server RSA Key đã được tạo",
                Location = new Point(20, 8),
                AutoSize = true,
                ForeColor = ThemeSuccess,
                Font = new Font("Segoe UI Semibold", 9.5f)
            };
            pnlStatus.Controls.Add(lblStatus);

            string publicKeyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{serverKeyPair.N}|{serverKeyPair.E}"));
            string displayKey = publicKeyBase64.Length > 50 ? publicKeyBase64.Substring(0, 50) + "…" : publicKeyBase64;

            Label lblServerInfo = new Label
            {
                Text = $"🔑 Public Key: {displayKey}   |   RSA 1024-bit   |   E = {serverKeyPair.E}",
                Location = new Point(20, 30),
                Size = new Size(1000, 18),
                ForeColor = ThemeTextMuted,
                Font = new Font("Cascadia Code", 7.5f)
            };
            pnlStatus.Controls.Add(lblServerInfo);

            Label lblInstruction = new Label
            {
                Text = "📌  START SERVER  →  Nhập dữ liệu  →  Gửi đến Server",
                Location = new Point(20, 48),
                Size = new Size(700, 18),
                ForeColor = ThemeWarn,
                Font = new Font("Segoe UI", 8, FontStyle.Italic)
            };
            pnlStatus.Controls.Add(lblInstruction);
        }

        private void InitializeInputTab(TabPage tab)
        {
            Label lblInfo = new Label
            {
                Text = "CLIENT:  Nhập dữ liệu và gửi đến Server qua kênh truyền đã mã hóa",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = ThemeAccentHover
            };
            tab.Controls.Add(lblInfo);

            int y = 40;
            int labelX = 20, textX = 200, width = 400;

            // User authentication section (Merged with Data section)
            Label lblDataSection = new Label
            {
                Text = "📝 Nhập thông tin tài khoản và dữ liệu nhạy cảm:",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeAccentHover
            };
            tab.Controls.Add(lblDataSection);
            y += 25;

            // Username
            Label lblUsername = new Label
            {
                Text = "🔑  Username:",
                Location = new Point(labelX, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeTextSecondary
            };
            tab.Controls.Add(lblUsername);

            txtUsername = new TextBox
            {
                Location = new Point(textX, y),
                Size = new Size(width, 28),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            tab.Controls.Add(txtUsername);
            y += 35;

            // Password
            Label lblPassword = new Label
            {
                Text = "🔒  Password:",
                Location = new Point(labelX, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeTextSecondary
            };
            tab.Controls.Add(lblPassword);

            txtPassword = new TextBox
            {
                Location = new Point(textX, y),
                Size = new Size(width, 28),
                PasswordChar = '●',
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            tab.Controls.Add(txtPassword);
            y += 35;

            // Create themed input fields
            TextBox[] fields = new TextBox[6];
            string[] labels = { "👤  Họ và tên:", "📧  Email:", "📱  Số điện thoại:", "💳  Thẻ tín dụng:", "🆔  SSN:", "🏠  Địa chỉ:" };
            string[] defaults = { "Huỳnh Ngọc Hải", "mr.haihuynhngoc@gmail.com", "0969074102", "4532-7797-9668-8888", "017204001788", "Học viện Kỹ Thuật Mật Mã" };

            for (int i = 0; i < 6; i++)
            {
                Label lbl = new Label
                {
                    Text = labels[i],
                    Location = new Point(labelX, y + 3),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = ThemeTextSecondary
                };
                tab.Controls.Add(lbl);

                bool isMultiline = (i == 5); // Address
                TextBox txt = new TextBox
                {
                    Location = new Point(textX, y),
                    Size = new Size(width, isMultiline ? 60 : 28),
                    Text = defaults[i],
                    Multiline = isMultiline,
                    BackColor = ThemeInput,
                    ForeColor = ThemeTextPrimary,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5f)
                };
                tab.Controls.Add(txt);
                fields[i] = txt;

                y += isMultiline ? 70 : 35;
            }

            txtName = fields[0]; txtEmail = fields[1]; txtPhone = fields[2];
            txtCard = fields[3]; txtSSN = fields[4]; txtAddress = fields[5];

            // Masking Type Selection
            y += 5;
            Label lblMaskType = new Label
            {
                Text = "🎭 Masking Method:",
                Location = new Point(labelX, y + 4),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeWarn
            };
            tab.Controls.Add(lblMaskType);

            cboMaskingType = new ComboBox
            {
                Location = new Point(textX, y),
                Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary
            };
            cboMaskingType.Items.AddRange(new object[]
            {
                "Che mặt nạ ký tự",
                "Xáo trộn dữ liệu",
                "Thay thế bằng dữ liệu giả",
                "Thêm nhiễu vào ký tự số"
            });
            cboMaskingType.SelectedIndex = 0; // Default: Che mặt nạ ký tự (masking type 1)
            tab.Controls.Add(cboMaskingType);

            y += 40;
            btnAdd = CreateModernButton("🚀  Gửi đến Server", ThemeAccent, Color.White, 180, 38);
            btnAdd.Location = new Point(textX, y);
            btnAdd.Font = new Font("Segoe UI Semibold", 10);
            btnAdd.Click += BtnSendToServer_Click;
            tab.Controls.Add(btnAdd);

            Button btnClear = CreateModernButton("✕  Xóa form", Color.FromArgb(70, 73, 95), ThemeTextSecondary, 100, 38);
            btnClear.Location = new Point(textX + 190, y);
            btnClear.Click += (s, e) => ClearInputs();
            tab.Controls.Add(btnClear);

            Button btnMockData = CreateModernButton("🎲  Mock Data", Color.FromArgb(80, 60, 120), Color.FromArgb(200, 180, 255), 120, 38);
            btnMockData.Location = new Point(textX + 300, y);
            btnMockData.Font = new Font("Segoe UI Semibold", 9);
            btnMockData.Click += (s, e) => FillMockData();
            tab.Controls.Add(btnMockData);

            // Response area
            y += 50;
            Label lblResponse = new Label
            {
                Text = "📩  Response từ Server:",
                Location = new Point(labelX, y + 2),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeSuccess
            };
            tab.Controls.Add(lblResponse);

            Button btnOpenResponse = CreateModernButton("🔍 Mở rộng", Color.FromArgb(55, 58, 80), ThemeTextPrimary, 100, 25);
            btnOpenResponse.Location = new Point(labelX + 200, y);
            btnOpenResponse.Font = new Font("Segoe UI", 8);
            tab.Controls.Add(btnOpenResponse);

            TextBox txtResponse = new TextBox
            {
                Name = "txtResponse",
                Location = new Point(labelX, y + 28),
                Size = new Size(850, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(25, 28, 40),
                ForeColor = ThemeSuccess,
                Font = new Font("Cascadia Code", 8.5f),
                BorderStyle = BorderStyle.None
            };
            tab.Controls.Add(txtResponse);

            btnOpenResponse.Click += (s, e) =>
            {
                Form responseForm = new Form
                {
                    Text = "📩 Response từ Server",
                    Size = new Size(700, 500),
                    StartPosition = FormStartPosition.CenterParent,
                    MinimumSize = new Size(400, 300),
                    BackColor = ThemeBg
                };
                TextBox txtFull = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    BackColor = Color.FromArgb(20, 22, 35),
                    ForeColor = ThemeSuccess,
                    Font = new Font("Cascadia Code", 10),
                    Text = txtResponse.Text,
                    WordWrap = false,
                    BorderStyle = BorderStyle.None
                };
                responseForm.Controls.Add(txtFull);
                responseForm.Show();
            };
        }

        private void InitializeOriginalTab(TabPage tab)
        {
            dgvOriginal = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(890, 510),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            StyleDataGridView(dgvOriginal);
            dgvOriginal.CellDoubleClick += ShowRecordDetail_CellDoubleClick;
            tab.Controls.Add(dgvOriginal);

            btnRefresh = CreateModernButton("🔄  Làm mới", Color.FromArgb(55, 58, 80), ThemeTextPrimary, 110, 30);
            btnRefresh.Location = new Point(10, 8);
            btnRefresh.Click += (s, e) => LoadOriginalData();
            tab.Controls.Add(btnRefresh);

            btnDelete = CreateModernButton("🗑️  Xóa dòng", ThemeDanger, Color.White, 110, 30);
            btnDelete.Location = new Point(130, 8);
            btnDelete.Click += BtnDelete_Click;
            tab.Controls.Add(btnDelete);
        }

        private void InitializeMaskedTab(TabPage tab)
        {
            Button btnShowMasked = CreateModernButton("🎭  Hiển thị Masked", ThemeAccent, Color.White, 170, 28);
            btnShowMasked.Location = new Point(10, 8);
            tab.Controls.Add(btnShowMasked);

            Label lblMaskTypeView = new Label
            {
                Text = "Phương thức:",
                Location = new Point(200, 12),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeTextSecondary
            };
            tab.Controls.Add(lblMaskTypeView);

            cboMaskingTypeView = new ComboBox
            {
                Location = new Point(290, 8),
                Size = new Size(220, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary
            };
            cboMaskingTypeView.Items.AddRange(new object[]
            {
                "Che mặt nạ ký tự",
                "Xáo trộn dữ liệu",
                "Thay thế bằng dữ liệu giả",
                "Thêm nhiễu vào ký tự số"
            });
            cboMaskingTypeView.SelectedIndex = 0;
            tab.Controls.Add(cboMaskingTypeView);

            DataGridView dgvMasked = new DataGridView
            {
                Location = new Point(10, 42),
                Size = new Size(890, 515),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            StyleDataGridView(dgvMasked);
            dgvMasked.CellDoubleClick += ShowRecordDetail_CellDoubleClick;
            tab.Controls.Add(dgvMasked);

            btnShowMasked.Click += (s, e) => ShowMaskedData(dgvMasked);
        }

        private void InitializeEncryptedTab(TabPage tab)
        {
            Label lblType = new Label
            {
                Text = "🔐  Phương thức mã hóa:",
                Location = new Point(10, 13),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = ThemeTextSecondary
            };
            tab.Controls.Add(lblType);

            cboEncryptionType = new ComboBox
            {
                Location = new Point(170, 9),
                Size = new Size(100, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary
            };
            cboEncryptionType.Items.AddRange(new object[] { "AES", "RSA", "Hybrid" });
            cboEncryptionType.SelectedIndex = 0;
            tab.Controls.Add(cboEncryptionType);

            btnEncrypt = CreateModernButton("🔒  Mã hóa", ThemeAccent, Color.White, 110, 30);
            btnEncrypt.Location = new Point(280, 8);
            btnEncrypt.Click += BtnEncrypt_Click;
            tab.Controls.Add(btnEncrypt);

            Button btnRefreshEnc = CreateModernButton("🔄  Làm mới", Color.FromArgb(55, 58, 80), ThemeTextPrimary, 110, 30);
            btnRefreshEnc.Location = new Point(400, 8);
            btnRefreshEnc.Click += (s, e) => LoadEncryptedData();
            tab.Controls.Add(btnRefreshEnc);

            dgvEncrypted = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(890, 510),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            StyleDataGridView(dgvEncrypted);
            dgvEncrypted.CellDoubleClick += ShowRecordDetail_CellDoubleClick;
            tab.Controls.Add(dgvEncrypted);
        }

        private void ShowRecordDetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && sender is DataGridView dgv)
            {
                var row = dgv.Rows[e.RowIndex];

                // Build a nicely formatted string representing JSON-like structured data
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    string colName = dgv.Columns[i].HeaderText;
                    string val = row.Cells[i].Value?.ToString() ?? "null";

                    // Format appropriately: strings in quotes unless it looks like a number/boolean
                    bool isNumeric = int.TryParse(val, out _) || double.TryParse(val, out _);
                    string formattedVal = (val == "null" || isNumeric) ? val : $"\"{val.Replace("\"", "\\\"")}\"";

                    sb.AppendLine($"  \"{colName}\": {formattedVal}" + (i < dgv.Columns.Count - 1 ? "," : ""));
                }
                sb.AppendLine("}");

                // Show it in a popup
                Form detailForm = new Form
                {
                    Text = "🔍 Chi tiết bản ghi",
                    Size = new Size(600, 450),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = ThemeBg,
                    ShowIcon = false
                };

                TextBox txtDetail = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    ReadOnly = true,
                    BackColor = Color.FromArgb(20, 22, 35),
                    ForeColor = ThemeCyan,
                    Font = new Font("Cascadia Code", 10),
                    Text = sb.ToString(),
                    BorderStyle = BorderStyle.None,
                    Margin = new Padding(10)
                };

                detailForm.Controls.Add(txtDetail);
                detailForm.ShowDialog();
            }
        }

        private void InitializeKeysTab(TabPage tab)
        {
            Label lblTitle = new Label
            {
                Text = "🔑  CRYPTOGRAPHIC KEYS",
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            tab.Controls.Add(lblTitle);

            // RSA Public Key
            Label lblRSAPublic = new Label
            {
                Text = "RSA PUBLIC KEY (Server):",
                Location = new Point(20, 55),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeWarn
            };
            tab.Controls.Add(lblRSAPublic);

            TextBox txtRSAPublic = new TextBox
            {
                Location = new Point(20, 80),
                Size = new Size(860, 110),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Cascadia Code", 9),
                BackColor = Color.FromArgb(25, 28, 40),
                ForeColor = ThemeSuccess,
                BorderStyle = BorderStyle.None,
                Text = serverKeyPair.GetPublicKeyPEM()
            };
            tab.Controls.Add(txtRSAPublic);

            Label lblRSAInfo = new Label
            {
                Text = $"📊  1024-bit  |  Modulus: {serverKeyPair.N.ToString().Length} digits  |  E = {serverKeyPair.E}",
                Location = new Point(20, 195),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ThemeTextMuted
            };
            tab.Controls.Add(lblRSAInfo);

            // Base64
            Label lblBase64 = new Label
            {
                Text = "PUBLIC KEY (Base64):",
                Location = new Point(20, 225),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeWarn
            };
            tab.Controls.Add(lblBase64);

            TextBox txtBase64 = new TextBox
            {
                Location = new Point(20, 250),
                Size = new Size(860, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Cascadia Code", 8),
                BackColor = Color.FromArgb(25, 28, 40),
                ForeColor = ThemeCyan,
                BorderStyle = BorderStyle.None,
                Text = serverKeyPair.GetPublicKeyBase64()
            };
            tab.Controls.Add(txtBase64);

            // Giải thích
            Label lblExplain = new Label
            {
                Text = "📌  Giải thích:\n" +
                       "• Public Key được chia sẻ công khai để Client mã hóa dữ liệu\n" +
                       "• Private Key được giữ bí mật ở Server để giải mã\n" +
                       "• Format Base64 giúp dễ dàng truyền qua mạng và lưu trữ\n" +
                       "• RSA 1024-bit đủ mạnh cho mục đích học tập (Production nên dùng 2048-bit+)",
                Location = new Point(20, 340),
                Size = new Size(860, 100),
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeTextSecondary
            };
            tab.Controls.Add(lblExplain);

            Button btnCopyPublic = CreateModernButton("📋  Copy Public Key", Color.FromArgb(55, 58, 80), ThemeTextPrimary, 160, 32);
            btnCopyPublic.Location = new Point(20, 450);
            btnCopyPublic.Click += (s, e) =>
            {
                System.Windows.Forms.Clipboard.SetText(serverKeyPair.GetPublicKeyBase64());
                MessageBox.Show("Đã copy Public Key vào clipboard!", "Thông báo");
            };
            tab.Controls.Add(btnCopyPublic);
        }

        private void BtnInitDB_Click(object sender, EventArgs e)
        {
            try
            {
                // Parse connection string
                var parts = txtConnectionString.Text.Split(';');
                string server = "localhost", database = "datamasking_db", user = "root", password = "";
                int port = 3306;

                foreach (var part in parts)
                {
                    if (part.StartsWith("Server=")) server = part.Split('=')[1];
                    else if (part.StartsWith("Port=")) port = int.Parse(part.Split('=')[1]);
                    else if (part.StartsWith("Database=")) database = part.Split('=')[1];
                    else if (part.StartsWith("Uid=")) user = part.Split('=')[1];
                    else if (part.StartsWith("Pwd=")) password = part.Split('=')[1];
                }

                dbManager = new DatabaseManager(server, database, user, password, port);
                dbManager.InitializeDatabase();

                lblStatus.Text = "Khởi tạo database thành công!";
                lblStatus.ForeColor = Color.Green;
                MessageBox.Show("Database đã được khởi tạo thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi khởi tạo database!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddSample_Click(object sender, EventArgs e)
        {
            try
            {
                dbManager.InsertSampleData();
                lblStatus.Text = "Đã thêm dữ liệu mẫu!";
                lblStatus.ForeColor = Color.Green;
                LoadOriginalData();
                MessageBox.Show("Đã thêm 3 bản ghi mẫu!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Thông báo");
                    return;
                }

                int id = dbManager.InsertSensitiveData(
                    txtName.Text, txtEmail.Text, txtPhone.Text,
                    txtCard.Text, txtSSN.Text, txtAddress.Text,
                    txtUsername.Text, Utils.MD5Hash.ComputeHash(txtPassword.Text)
                );

                lblStatus.Text = $"Đã thêm bản ghi ID: {id}";
                lblStatus.ForeColor = Color.Green;
                ClearInputs();
                LoadOriginalData();
                MessageBox.Show("Thêm dữ liệu thành công!", "Thành công");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi thêm dữ liệu!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void BtnSendToServer_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên, username và password!", "Thông báo");
                    return;
                }

                if (txtPassword.Text.Length <= 6)
                {
                    MessageBox.Show("Mật khẩu phải lớn hơn 6 ký tự!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }

                if (!isServerRunning)
                {
                    MessageBox.Show("Server chưa được khởi động! Vui lòng Start Server trước.", "Thông báo");
                    return;
                }

                // Clear log trước
                txtTransmissionLog.Clear();

                // Parse masking type từ ComboBox
                MaskingType selectedMaskingType = (MaskingType)cboMaskingType.SelectedIndex;

                // CLIENT: Gửi request qua TCP
                Task.Run(async () =>
                {
                    ServerResponse response = await clientService.SendSecureRequestAsync(
                        txtUsername.Text, txtPassword.Text, txtName.Text, txtEmail.Text, txtPhone.Text,
                        txtCard.Text, txtSSN.Text, txtAddress.Text,
                        selectedMaskingType
                    );

                    // Update UI trên main thread
                    this.Invoke(new Action(() =>
                    {
                        if (response.Success)
                        {
                            // Tạo JSON response
                            var jsonResponse = new
                            {
                                success = true,
                                recordId = response.RecordId,
                                message = response.Message,
                                maskedData = new
                                {
                                    fullName = response.MaskedData.FullName,
                                    email = response.MaskedData.Email,
                                    phone = response.MaskedData.Phone,
                                    creditCard = response.MaskedData.CreditCard,
                                    ssn = response.MaskedData.SSN,
                                    address = response.MaskedData.Address
                                }
                            };

                            string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonResponse, new System.Text.Json.JsonSerializerOptions
                            {
                                WriteIndented = true,
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            });

                            // Hiển thị response
                            var txtResponse = tabControl.TabPages[0].Controls.Find("txtResponse", false)[0] as TextBox;
                            if (txtResponse != null)
                            {
                                txtResponse.Text = jsonString;
                            }

                            lblStatus.Text = $"✓ Đã gửi và nhận response qua TCP! ID: {response.RecordId}";
                            lblStatus.ForeColor = Color.Green;
                            ClearInputs();
                            LoadOriginalData();
                        }
                        else
                        {
                            MessageBox.Show(response.Message, "Lỗi");
                            lblStatus.Text = "✗ Lỗi xử lý request!";
                            lblStatus.ForeColor = Color.Red;
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                lblStatus.Text = "✗ Lỗi gửi dữ liệu!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private async void BtnStartServer_Click(object sender, EventArgs e)
        {
            try
            {
                // Hiển thị progress
                progressServer.Visible = true;
                var lblProgress = this.Controls.Find("lblProgress", true).FirstOrDefault() as Label;
                if (lblProgress != null)
                {
                    lblProgress.Text = "Đang khởi động server...";
                }

                btnStartServer.Enabled = false;
                lblServerStatus.Text = "● Server: STARTING...";
                lblServerStatus.ForeColor = Color.Orange;

                // Chờ một chút để UI update
                await System.Threading.Tasks.Task.Delay(500);

                // Start server
                await System.Threading.Tasks.Task.Run(() => serverService.Start(8888));
                isServerRunning = true;

                // Ẩn progress
                progressServer.Visible = false;
                if (lblProgress != null)
                {
                    lblProgress.Text = "Server đã sẵn sàng!";
                }

                btnStopServer.Enabled = true;
                lblServerStatus.Text = "● Server: RUNNING (Port 8888)";
                lblServerStatus.ForeColor = Color.Green;

                lblStatus.Text = "✓ Server đã khởi động thành công trên port 8888!";
                lblStatus.ForeColor = Color.Green;

                // Mở popup Server Log
                OpenServerLogWindow();

                // Subscribe server log
                TransmissionLogger.OnServerLog += LogServerMessage;

                // Xóa message sau 3 giây
                await System.Threading.Tasks.Task.Delay(3000);
                if (lblProgress != null)
                {
                    lblProgress.Text = "";
                }
            }
            catch (Exception ex)
            {
                progressServer.Visible = false;
                btnStartServer.Enabled = true;
                lblServerStatus.Text = "● Server: ERROR";
                lblServerStatus.ForeColor = Color.Red;
                MessageBox.Show("Lỗi khởi động server: " + ex.Message, "Lỗi");
            }
        }

        private void OpenServerLogWindow()
        {
            serverLogForm = new Form
            {
                Text = "🖥️ SERVER LOG - Port 8888",
                Size = new Size(750, 550),
                // Khởi động sẽ ở góc dưới bên phải 
                StartPosition = FormStartPosition.Manual,
                Location = new Point(Screen.PrimaryScreen.Bounds.Width - 760, Screen.PrimaryScreen.Bounds.Height - 560),
                MinimumSize = new Size(500, 350),
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Header
            Label lblHeader = new Label
            {
                Text = "● SERVER RUNNING - Transmission Log",
                Dock = DockStyle.Top,
                Height = 35,
                ForeColor = Color.LimeGreen,
                BackColor = Color.FromArgb(20, 20, 20),
                Font = new Font("Consolas", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            serverLogForm.Controls.Add(lblHeader);

            // Button panel
            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            serverLogForm.Controls.Add(pnlButtons);

            Button btnClearServerLog = new Button
            {
                Text = "Xóa Log",
                Location = new Point(10, 5),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            btnClearServerLog.Click += (s, ev) => txtServerLog?.Clear();
            pnlButtons.Controls.Add(btnClearServerLog);

            Button btnStopFromLog = new Button
            {
                Text = "⏹ Stop Server",
                Location = new Point(100, 5),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.DarkRed,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnStopFromLog.Click += (s, ev) => serverLogForm?.Close();
            pnlButtons.Controls.Add(btnStopFromLog);

            // Server log textbox
            txtServerLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9),
                WordWrap = false
            };
            serverLogForm.Controls.Add(txtServerLog);

            // Ensure correct z-order (textbox fills between header and button panel)
            txtServerLog.BringToFront();

            // Khi đóng cửa sổ server log => tự động stop server
            serverLogForm.FormClosed += (s, ev) =>
            {
                TransmissionLogger.OnServerLog -= LogServerMessage;
                txtServerLog = null;

                if (isServerRunning)
                {
                    serverService.Stop();
                    isServerRunning = false;

                    // Update UI trên main form
                    if (!this.IsDisposed)
                    {
                        btnStartServer.Enabled = true;
                        btnStopServer.Enabled = false;
                        lblServerStatus.Text = "● Server: STOPPED";
                        lblServerStatus.ForeColor = Color.Red;
                        lblStatus.Text = "Server đã dừng (đóng cửa sổ log)";
                        lblStatus.ForeColor = Color.Blue;
                    }
                }
                serverLogForm = null;
            };

            serverLogForm.Show();
        }

        private void BtnStopServer_Click(object sender, EventArgs e)
        {
            try
            {
                // Đóng popup server log (sẽ tự động stop server qua FormClosed)
                if (serverLogForm != null && !serverLogForm.IsDisposed)
                {
                    serverLogForm.Close();
                }
                else
                {
                    // Fallback nếu popup đã bị đóng
                    serverService.Stop();
                    isServerRunning = false;

                    btnStartServer.Enabled = true;
                    btnStopServer.Enabled = false;
                    lblServerStatus.Text = "● Server: STOPPED";
                    lblServerStatus.ForeColor = Color.Red;

                    lblStatus.Text = "Server đã dừng";
                    lblStatus.ForeColor = Color.Blue;

                    var lblProgress = this.Controls.Find("lblProgress", true).FirstOrDefault() as Label;
                    if (lblProgress != null)
                    {
                        lblProgress.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi dừng server: " + ex.Message, "Lỗi");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvOriginal.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn dòng cần xóa!", "Thông báo");
                    return;
                }

                int id = Convert.ToInt32(dgvOriginal.SelectedRows[0].Cells["id"].Value);

                if (MessageBox.Show("Bạn có chắc muốn xóa?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    dbManager.DeleteSensitiveData(id);
                    lblStatus.Text = "Đã xóa bản ghi!";
                    lblStatus.ForeColor = Color.Green;
                    LoadOriginalData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void BtnEncrypt_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = dbManager.GetAllSensitiveData();
                string encType = cboEncryptionType.SelectedItem.ToString();
                int count = 0;

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["id"]);
                    string encrypted = maskingService.EncryptRecord(
                        row["full_name"].ToString(),
                        row["email"].ToString(),
                        row["phone"].ToString(),
                        row["credit_card"].ToString(),
                        row["ssn"].ToString(),
                        row["address"].ToString(),
                        encType
                    );

                    dbManager.InsertEncryptedData(id, encrypted, encType);
                    count++;
                }

                lblStatus.Text = $"Đã mã hóa {count} bản ghi bằng {encType}!";
                lblStatus.ForeColor = Color.Green;
                LoadEncryptedData();
                MessageBox.Show($"Đã mã hóa {count} bản ghi!", "Thành công");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi mã hóa!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void LoadOriginalData()
        {
            try
            {
                dgvOriginal.DataSource = dbManager.GetAllSensitiveData();
                lblStatus.Text = "Đã tải dữ liệu gốc";
                lblStatus.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void LoadEncryptedData()
        {
            try
            {
                dgvEncrypted.DataSource = dbManager.GetAllEncryptedData();
                lblStatus.Text = "Đã tải dữ liệu mã hóa";
                lblStatus.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void ShowMaskedData(DataGridView dgv)
        {
            try
            {
                // Parse masking type từ ComboBox trên tab Masked
                MaskingType maskType = (MaskingType)cboMaskingTypeView.SelectedIndex;

                string[] maskTypeNames = { "Che mặt nạ ký tự", "Xáo trộn dữ liệu", "Dữ liệu giả", "Nhiễu số" };
                string typeName = maskTypeNames[(int)maskType];

                DataTable dt = dbManager.GetAllSensitiveData();
                DataTable maskedDt = new DataTable();

                maskedDt.Columns.Add("ID");
                maskedDt.Columns.Add($"Họ tên ({typeName})");
                maskedDt.Columns.Add($"Email ({typeName})");
                maskedDt.Columns.Add($"Điện thoại ({typeName})");
                maskedDt.Columns.Add($"Thẻ tín dụng ({typeName})");
                maskedDt.Columns.Add($"SSN ({typeName})");
                maskedDt.Columns.Add($"Địa chỉ ({typeName})");

                foreach (DataRow row in dt.Rows)
                {
                    maskedDt.Rows.Add(
                        row["id"],
                        maskingService.ApplyMasking(row["full_name"].ToString(), "name", maskType),
                        maskingService.ApplyMasking(row["email"].ToString(), "email", maskType),
                        maskingService.ApplyMasking(row["phone"].ToString(), "phone", maskType),
                        maskingService.ApplyMasking(row["credit_card"].ToString(), "creditcard", maskType),
                        maskingService.ApplyMasking(row["ssn"].ToString(), "ssn", maskType),
                        maskingService.ApplyMasking(row["address"].ToString(), "address", maskType)
                    );
                }

                dgv.DataSource = maskedDt;
                lblStatus.Text = $"Đã hiển thị dữ liệu che giấu - Phương thức: {typeName}";
                lblStatus.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void ClearInputs()
        {
            txtUsername.Clear();
            txtPassword.Clear();
            txtName.Clear();
            txtEmail.Clear();
            txtPhone.Clear();
            txtCard.Clear();
            txtSSN.Clear();
            txtAddress.Clear();
        }

        private void FillMockData()
        {
            var rand = new Random();
            
            string[] firstNames = { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý" };
            string[] middleNames = { "Văn", "Hữu", "Thanh", "Công", "Minh", "Thị", "Ngọc", "Thu", "Mai", "Hải", "Tuấn", "Hoàng", "Hồng", "Xuân" };
            string[] lastNames = { "Nam", "Anh", "Hải", "Sơn", "Hùng", "Bình", "Thảo", "Lan", "Hương", "Hà", "Trang", "Linh", "Quỳnh", "Khoa", "Đạt", "Long" };
            
            string first = firstNames[rand.Next(firstNames.Length)];
            string middle = middleNames[rand.Next(middleNames.Length)];
            string last = lastNames[rand.Next(lastNames.Length)];
            
            string fullName = $"{first} {middle} {last}";
            
            // Remove diacritics for username and email
            string normalizedName = RemoveVietnameseTone(first).ToLower() + RemoveVietnameseTone(middle).ToLower()[0] + RemoveVietnameseTone(last).ToLower();
            
            string[] domains = { "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "fpt.edu.vn", "vnu.edu.vn" };
            string email = $"{normalizedName}{rand.Next(10, 999)}@{domains[rand.Next(domains.Length)]}";
            
            string[] prefixes = { "090", "091", "092", "093", "094", "096", "097", "098", "099", "086", "088", "089", "032", "033", "034", "035", "036", "037", "038", "039" };
            string phone = prefixes[rand.Next(prefixes.Length)] + rand.Next(1000000, 9999999).ToString();
            
            string card = $"{rand.Next(4000, 5999)}-{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}";
            
            string ssn = $"0{rand.Next(1, 99):D2}2{rand.Next(0, 9)}0{rand.Next(100000, 999999)}"; // Typical Vietnam CCCD format start
            
            string[] streets = { "Khúc Thừa Dụ", "Trần Duy Hưng", "Nguyễn Trãi", "Lê Văn Lương", "Láng Hạ", "Kim Mã", "Cầu Giấy", "Hồ Tùng Mậu", "Tôn Đức Thắng", "Phố Huế" };
            string[] cities = { "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ", "Nha Trang", "Vũng Tàu", "Huế" };
            string address = $"Số {rand.Next(1, 999)} {streets[rand.Next(streets.Length)]}, {cities[rand.Next(cities.Length)]}";

            txtUsername.Text = normalizedName;
            // password is left intact or untouched per requirement "trừ password"
            txtName.Text = fullName;
            txtEmail.Text = email;
            txtPhone.Text = phone;
            txtCard.Text = card;
            txtSSN.Text = ssn;
            txtAddress.Text = address;

            // Chuyển Focus về trường password sau khi mock để user nhập pass
            txtPassword.Focus();
        }

        public static string RemoveVietnameseTone(string text)
        {
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };
            
            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
            return text;
        }
    }
}


