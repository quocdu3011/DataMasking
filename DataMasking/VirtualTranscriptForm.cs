using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using DataMasking.Database;
using DataMasking.Network;
using DataMasking.Key;
using DataMasking.Utils;
using DataMasking.Masking;

namespace DataMasking
{
    public class VirtualTranscriptForm : Form
    {
        private StudentDatabaseManager dbManager;
        private StudentClientService clientService;
        private RSAKeyPair serverKeyPair;
        private MaskingService maskingService; // Thêm masking service
        
        private Label lblStudentInfo;
        private DataGridView dgvScores;
        private Label lblTotalCredits;
        private Label lblPredictedCPA;
        private Button btnLoadScores;
        private Button btnSelectAll;
        private Button btnSaveToServer;
        private Button btnGradeTable;
        private RichTextBox txtClientLog;
        
        private bool isSelectAllMode = true; // Track select all/unselect all mode
        
        // Personal info labels (dòng 2 trong pnlInfo)
        private Label lblInfoBankAccount, lblInfoIdCard, lblInfoBirthPlace;
        private Label lblInfoPhone, lblInfoEmail, lblInfoEmergency;
        
        // Theme colors - Purple/Gold theme for Virtual Transcript
        private static readonly Color ThemeBg = Color.FromArgb(25, 20, 35);
        private static readonly Color ThemePanel = Color.FromArgb(45, 35, 60);
        private static readonly Color ThemeHeader = Color.FromArgb(35, 25, 50);
        private static readonly Color ThemeAccent = Color.FromArgb(156, 39, 176);
        private static readonly Color ThemeAccentHover = Color.FromArgb(186, 69, 206);
        private static readonly Color ThemeTextPrimary = Color.FromArgb(240, 235, 250);
        private static readonly Color ThemeTextSecondary = Color.FromArgb(180, 170, 200);
        private static readonly Color ThemeInput = Color.FromArgb(55, 45, 75);
        private static readonly Color ThemeSuccess = Color.FromArgb(129, 199, 132);
        private static readonly Color ThemeWarn = Color.FromArgb(255, 215, 0);
        private static readonly Color ThemeDanger = Color.FromArgb(244, 67, 54);

        public VirtualTranscriptForm()
        {
            dbManager = new StudentDatabaseManager("36.50.54.109", "kmalegend", "anonymous", "Thuanld@255", 3306);
            maskingService = new MaskingService(); // Khởi tạo masking service
            
            // Sử dụng server public key từ server đang chạy
            // Không tạo key pair mới để tránh lỗi giải mã
            serverKeyPair = null; // Sẽ được set từ bên ngoài
            clientService = new StudentClientService(null, "127.0.0.1", 8888);
            
            InitializeComponents();
            
            TransmissionLogger.OnClientLog += LogClient;
        }

        // Constructor nhận server key pair từ form chính
        public VirtualTranscriptForm(RSAKeyPair sharedServerKeyPair) : this()
        {
            serverKeyPair = sharedServerKeyPair;
            clientService = new StudentClientService(serverKeyPair, "127.0.0.1", 8888);
            
            // Auto-load scores when form opens
            this.Load += async (s, e) => await AutoLoadScores();
        }

        // Method để set server key pair sau khi khởi tạo
        public void SetServerKeyPair(RSAKeyPair sharedServerKeyPair)
        {
            serverKeyPair = sharedServerKeyPair;
            clientService = new StudentClientService(serverKeyPair, "127.0.0.1", 8888);
        }

        private void LogClient(string message)
        {
            if (txtClientLog != null && txtClientLog.InvokeRequired)
            {
                txtClientLog.Invoke(new Action<string>(LogClient), message);
            }
            else if (txtClientLog != null)
            {
                txtClientLog.AppendText(message + Environment.NewLine);
            }
        }

        private void InitializeComponents()
        {
            this.Text = "BẢNG ĐIỂM ẢO - KMA LEGEND";
            this.Size = new Size(1600, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ThemeBg;
            this.ForeColor = ThemeTextPrimary;
            this.Font = new Font("Segoe UI", 9);

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeHeader
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "✨ BẢNG ĐIỂM ẢO - TÍNH TOÁN CPA DỰ KIẾN",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0)
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "💎 Chọn môn học để tính CPA dự kiến • Mô phỏng kết quả học tập",
                Location = new Point(20, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = ThemeTextSecondary
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Control Panel
            Panel pnlControl = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(900, 80),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlControl);

            btnSelectAll = CreateModernButton("☑ Chọn tất cả", Color.FromArgb(103, 58, 183), ThemeTextPrimary, 130, 38);
            btnSelectAll.Location = new Point(20, 25);
            btnSelectAll.Click += BtnSelectAll_Click;
            pnlControl.Controls.Add(btnSelectAll);

            btnSaveToServer = CreateModernButton("💾 Lưu vào DB", Color.FromArgb(255, 193, 7), Color.FromArgb(50, 40, 30), 150, 38);
            btnSaveToServer.Location = new Point(170, 25);
            btnSaveToServer.Font = new Font("Segoe UI Semibold", 10);
            btnSaveToServer.Click += BtnSaveToServer_Click;
            pnlControl.Controls.Add(btnSaveToServer);

            btnGradeTable = CreateModernButton("📋 Bảng quy đổi", Color.FromArgb(103, 58, 183), ThemeTextPrimary, 140, 38);
            btnGradeTable.Location = new Point(340, 25);
            btnGradeTable.Click += BtnGradeTable_Click;
            pnlControl.Controls.Add(btnGradeTable);

            Button btnCPACalculator = CreateModernButton("🎯 Tính CPA mục tiêu", Color.FromArgb(255, 87, 34), Color.White, 150, 38);
            btnCPACalculator.Location = new Point(500, 25);
            btnCPACalculator.Click += BtnCPACalculator_Click;
            pnlControl.Controls.Add(btnCPACalculator);

            Button btnAddSubject = CreateModernButton("➕ Thêm môn mới", Color.FromArgb(76, 175, 80), Color.White, 140, 38);
            btnAddSubject.Location = new Point(670, 25);
            btnAddSubject.Click += BtnAddSubject_Click;
            pnlControl.Controls.Add(btnAddSubject);

            Button btnDeleteSubject = CreateModernButton("🗑️", Color.FromArgb(244, 67, 54), Color.White, 50, 38);
            btnDeleteSubject.Location = new Point(830, 25);
            btnDeleteSubject.Click += BtnDeleteSubject_Click;
            pnlControl.Controls.Add(btnDeleteSubject);

            Label lblNote = new Label
            {
                Text = "💡 Công thức: (0.3*CC + 0.7*GK)*0.3 + 0.7*CK",
                Location = new Point(20, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = ThemeWarn
            };
            pnlControl.Controls.Add(lblNote);

            // Client Log Panel
            Panel pnlClientLog = new Panel
            {
                Location = new Point(940, 80),
                Size = new Size(620, 680),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlClientLog);

            Label lblClientLog = new Label
            {
                Text = "💬 CLIENT TRANSMISSION LOG",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = Color.FromArgb(186, 104, 200)
            };
            pnlClientLog.Controls.Add(lblClientLog);

            Button btnClearClientLog = CreateModernButton("Xóa", Color.FromArgb(60, 63, 80), ThemeTextSecondary, 60, 24);
            btnClearClientLog.Location = new Point(550, 8);
            btnClearClientLog.Font = new Font("Segoe UI", 8);
            btnClearClientLog.Click += (s, e) => txtClientLog?.Clear();
            pnlClientLog.Controls.Add(btnClearClientLog);

            txtClientLog = new RichTextBox
            {
                Location = new Point(10, 40),
                Size = new Size(600, 630),
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 15, 30),
                ForeColor = Color.FromArgb(186, 104, 200),
                Font = new Font("Cascadia Code", 8.5f),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            pnlClientLog.Controls.Add(txtClientLog);

            // Student Info Panel - mở rộng để chứa cả thông tin cá nhân masked
            Panel pnlInfo = new Panel
            {
                Location = new Point(20, 175),
                Size = new Size(900, 65),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlInfo);

            lblStudentInfo = new Label
            {
                Text = "Nhấn 'Tải điểm' để bắt đầu...",
                Location = new Point(10, 8),
                Size = new Size(880, 22),
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeTextSecondary
            };
            pnlInfo.Controls.Add(lblStudentInfo);

            // Dòng 2: thông tin cá nhân masked
            lblInfoBankAccount = MakeInfoLabel("STK: --", 10, 36);
            lblInfoIdCard      = MakeInfoLabel("CMTND: --", 155, 36);
            lblInfoBirthPlace  = MakeInfoLabel("Nơi sinh: --", 310, 36);
            lblInfoPhone       = MakeInfoLabel("SĐT: --", 480, 36);
            lblInfoEmail       = MakeInfoLabel("Email: --", 590, 36);
            lblInfoEmergency   = MakeInfoLabel("Liên hệ khẩn: --", 730, 36);
            foreach (var lbl in new[] { lblInfoBankAccount, lblInfoIdCard, lblInfoBirthPlace,
                                         lblInfoPhone, lblInfoEmail, lblInfoEmergency })
                pnlInfo.Controls.Add(lbl);

            // Scores DataGridView with checkbox
            dgvScores = new DataGridView
            {
                Location = new Point(20, 255),
                Size = new Size(900, 395),
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            StyleDataGridView(dgvScores);
            dgvScores.CellValueChanged += DgvScores_CellValueChanged;
            dgvScores.CellEndEdit += DgvScores_CellEndEdit;
            dgvScores.CellContentClick += DgvScores_CellContentClick;
            this.Controls.Add(dgvScores);

            // Summary Panel
            Panel pnlSummary = new Panel
            {
                Location = new Point(20, 665),
                Size = new Size(900, 95),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlSummary);

            int col1X = 20;
            lblTotalCredits = new Label
            {
                Text = "Tổng tín chỉ đã hoàn thành: --",
                Location = new Point(col1X, 25),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeWarn
            };
            pnlSummary.Controls.Add(lblTotalCredits);

            int col2X = 400;
            lblPredictedCPA = new Label
            {
                Text = "CPA DỰ KIẾN: --",
                Location = new Point(col2X, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0)
            };
            pnlSummary.Controls.Add(lblPredictedCPA);

            // ── Personal Info Panel (bên phải, dưới client log)
            BuildPersonalInfoPanel();
        }

        private void BuildPersonalInfoPanel()
        {
            // Labels đã được tạo inline trong InitializeComponents, không cần panel riêng
        }

        private Label MakeInfoLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = ThemeTextSecondary
            };
        }

        private async Task LoadProfileData()
        {
            if (!SessionManager.IsLoggedIn || serverKeyPair == null) return;

            try
            {
                string username = SessionManager.Username;
                string password = SessionManager.Password;

                LogClient("[CLIENT] === LOADING ACTVN PROFILE ===");
                var response = await clientService.SendActvnProfileRequestAsync(username, password);

                if (response != null && response.Success && response.StudentInfo != null)
                {
                    var info = response.StudentInfo;
                    LogClient($"[CLIENT] Profile loaded: {info.StudentName}");

                    // Thông tin cá nhân nhạy cảm - hiển thị masked ở dòng 2 của pnlInfo
                    lblInfoBankAccount.Text = $"STK: {MaskMiddle(info.BankAccount, 4, 4)}";
                    lblInfoIdCard.Text      = $"CMTND: {MaskMiddle(info.IdCard, 3, 3)}";
                    lblInfoBirthPlace.Text  = $"Nơi sinh: {MaskBirthPlace(info.BirthPlace)}";
                    lblInfoPhone.Text       = $"SĐT: {MaskPhone(info.PersonalPhone)}";
                    lblInfoEmail.Text       = $"Email: {MaskEmail(info.Email)}";
                    lblInfoEmergency.Text   = $"Liên hệ khẩn: {MaskMiddle(info.EmergencyContact, 2, 0)}";

                    foreach (var lbl in new[] { lblInfoBankAccount, lblInfoIdCard,
                                                 lblInfoPhone, lblInfoEmail, lblInfoEmergency })
                        lbl.ForeColor = ThemeTextPrimary;

                    LogClient("[CLIENT] Profile display updated with masked data");
                }
                else
                {
                    string msg = response?.Message ?? "Không có phản hồi";
                    LogClient($"[CLIENT] Profile load failed: {msg}");
                    MessageBox.Show($"Không tải được thông tin cá nhân:\n{msg}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogClient($"[CLIENT] Profile error: {ex.Message}");
            }
        }

        // ── Masking helpers ──────────────────────────────────────────────────

        /// <summary>Giữ <paramref name="keepStart"/> ký tự đầu và <paramref name="keepEnd"/> ký tự cuối, mask phần giữa.</summary>
        private string MaskMiddle(string value, int keepStart, int keepEnd)
        {
            if (string.IsNullOrEmpty(value)) return "--";
            int maskLen = value.Length - keepStart - keepEnd;
            if (maskLen <= 0) return new string('*', value.Length);
            return value.Substring(0, keepStart)
                 + new string('*', maskLen)
                 + (keepEnd > 0 ? value.Substring(value.Length - keepEnd) : "");
        }

        private string MaskName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "--";
            var parts = name.Split(' ');
            // Giữ nguyên họ, mask tên đệm và tên
            if (parts.Length == 1) return parts[0][0] + new string('*', parts[0].Length - 1);
            string result = parts[0]; // họ giữ nguyên
            for (int i = 1; i < parts.Length - 1; i++)
                result += " " + (parts[i].Length > 0 ? parts[i][0] + new string('*', parts[i].Length - 1) : "");
            string lastName = parts[^1];
            result += " " + (lastName.Length > 0 ? lastName[0] + new string('*', lastName.Length - 1) : "");
            return result;
        }

        private string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4) return "--";
            return new string('*', phone.Length - 4) + phone.Substring(phone.Length - 4);
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "--";
            int at = email.IndexOf('@');
            if (at <= 1) return email;
            return email[0] + new string('*', at - 1) + email.Substring(at);
        }

        private string MaskBirthPlace(string place)
        {
            if (string.IsNullOrEmpty(place)) return "--";
            // Chỉ hiện 10 ký tự đầu
            return place.Length <= 10 ? place : place.Substring(0, 10) + "...";
        }

        private Button CreateModernButton(string text, Color bgColor, Color fgColor, int width, int height)        {
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
            return btn;
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = ThemePanel;
            dgv.GridColor = Color.FromArgb(70, 75, 95);
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
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 48, 65);
            dgv.RowTemplate.Height = 35;
        }

        private async Task AutoLoadScores()
        {
            // Check if logged in
            if (!SessionManager.IsLoggedIn)
            {
                ShowLoginDialog();
                return;
            }

            // Check if server key pair is available
            if (serverKeyPair == null)
            {
                MessageBox.Show("Lỗi: Không có server key pair!\n\nVui lòng khởi động server từ form chính trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load cả điểm và thông tin cá nhân song song
            await Task.WhenAll(LoadScoresData(), LoadProfileData());
        }

        private async Task LoadScoresData()
        {
            try
            {
                LogClient("=======================================================");
                LogClient("[CLIENT] === LOADING VIRTUAL SCORES DATA ===");
                
                // Get student code from session
                string studentCode = SessionManager.Username;
                LogClient($"[CLIENT] Student Code from Session: {studentCode}");

                // Send request to server via encrypted channel
                LogClient("[CLIENT] Sending virtual score request to server...");
                var response = await clientService.SendVirtualScoreRequestAsync(studentCode);
                LogClient($"[CLIENT] Server response received - Success: {response.Success}");
                LogClient($"[CLIENT] Response Items Count: {response.Items?.Count ?? 0}");

                // Get student info from database (for display only)
                LogClient("[CLIENT] Fetching student info from database...");
                var student = await Task.Run(() => dbManager.GetStudentByCode(studentCode));
                if (student != null)
                {
                    LogClient($"[CLIENT] Student found in DB: {student.StudentName} - {student.StudentClass}");
                    lblStudentInfo.Text = $"Mã SV: {student.StudentCode}  |  Họ tên: {student.StudentName}  |  Lớp: {student.StudentClass}";
                    lblStudentInfo.ForeColor = ThemeSuccess;
                }
                else
                {
                    LogClient("[CLIENT] Student not found in database");
                    lblStudentInfo.Text = $"Mã SV: {studentCode} (Chưa có thông tin)";
                    lblStudentInfo.ForeColor = ThemeWarn;
                }

                // Convert response to display format
                LogClient("[CLIENT] === PROCESSING SCORE DATA ===");
                var virtualScores = new List<VirtualScoreItem>();
                if (response.Success && response.Items != null)
                {
                    LogClient($"[CLIENT] Processing {response.Items.Count} score items...");
                    int selectedCount = 0;
                    int passedCount = 0;
                    int peCount = 0;
                    
                    foreach (var item in response.Items)
                    {
                        bool isPassed = item.ScoreOverall >= 4.0f;
                        bool isPE = IsPhysicalEducation(item.SubjectName);
                        
                        if (item.IsSelected) selectedCount++;
                        if (isPassed) passedCount++;
                        if (isPE) peCount++;

                        virtualScores.Add(new VirtualScoreItem
                        {
                            Chọn = item.IsSelected,
                            Môn_học = item.SubjectName,
                            Tín_chỉ = item.SubjectCredit,
                            Điểm_CC = item.ScoreFirst,
                            Điểm_GK = item.ScoreSecond,
                            Điểm_CK = item.ScoreFinal,
                            Điểm_TK = item.ScoreOverall,
                            Điểm_chữ = item.ScoreText,
                            Trạng_thái = isPassed ? "✓" : "✗",
                            IsPassed = isPassed,
                            IsCurrentSemester = false,
                            IsPE = isPE
                        });
                    }
                    
                    LogClient($"[CLIENT] Score Statistics:");
                    LogClient($"[CLIENT] - Total subjects: {response.Items.Count}");
                    LogClient($"[CLIENT] - Selected subjects: {selectedCount}");
                    LogClient($"[CLIENT] - Passed subjects: {passedCount}");
                    LogClient($"[CLIENT] - PE subjects: {peCount}");
                }
                else
                {
                    LogClient("[CLIENT] No score data received or request failed");
                }

                LogClient("[CLIENT] === CONFIGURING DATA GRID ===");
                dgvScores.DataSource = virtualScores;
                LogClient($"[CLIENT] DataSource set with {virtualScores.Count} items");

                // Configure columns
                if (dgvScores.Columns.Contains("Chọn"))
                {
                    dgvScores.Columns["Chọn"].ReadOnly = false;
                    dgvScores.Columns["Chọn"].Width = 50;
                    LogClient("[CLIENT] Configured 'Chọn' column");
                }

                // Make editable columns
                string[] editableColumns = { "Môn_học", "Tín_chỉ", "Điểm_CC", "Điểm_GK", "Điểm_CK" };
                foreach (string colName in editableColumns)
                {
                    if (dgvScores.Columns.Contains(colName))
                    {
                        dgvScores.Columns[colName].ReadOnly = false;
                    }
                }
                LogClient($"[CLIENT] Configured {editableColumns.Length} editable columns");

                // Hide helper columns
                string[] hiddenColumns = { "IsPassed", "IsCurrentSemester", "IsPE" };
                foreach (string colName in hiddenColumns)
                {
                    if (dgvScores.Columns.Contains(colName))
                    {
                        dgvScores.Columns[colName].Visible = false;
                    }
                }
                LogClient($"[CLIENT] Hidden {hiddenColumns.Length} helper columns");

                // Make calculated columns readonly
                string[] readonlyColumns = { "Điểm_TK", "Điểm_chữ", "Trạng_thái" };
                foreach (string colName in readonlyColumns)
                {
                    if (dgvScores.Columns.Contains(colName))
                    {
                        dgvScores.Columns[colName].ReadOnly = true;
                    }
                }
                LogClient($"[CLIENT] Set {readonlyColumns.Length} calculated columns as readonly");

                // Highlight rows
                LogClient("[CLIENT] === APPLYING ROW STYLING ===");
                int peRowCount = 0;
                int failedRowCount = 0;
                
                foreach (DataGridViewRow row in dgvScores.Rows)
                {
                    if (row.Cells.Count == 0) continue;
                    
                    try
                    {
                        bool isPassed = row.Cells["IsPassed"].Value != null && (bool)row.Cells["IsPassed"].Value;
                        bool isPE = row.Cells["IsPE"].Value != null && (bool)row.Cells["IsPE"].Value;

                        if (isPE)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(60, 60, 70);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 160);
                            peRowCount++;
                        }
                        else if (!isPassed)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(120, 40, 40);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 200, 200);
                            failedRowCount++;
                        }
                    }
                    catch { }
                }
                
                LogClient($"[CLIENT] Applied styling - PE rows: {peRowCount}, Failed rows: {failedRowCount}");

                // Calculate predicted CPA
                LogClient("[CLIENT] === CALCULATING PREDICTED CPA ===");
                CalculatePredictedCPA();
                LogClient("[CLIENT] CPA calculation completed");

                if (virtualScores.Count == 0)
                {
                    LogClient("[CLIENT] No virtual scores found - showing info message");
                    MessageBox.Show("Chưa có dữ liệu!\n\nBấm 'Thêm môn mới' để bắt đầu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                LogClient("[CLIENT] === LOAD COMPLETED SUCCESSFULLY ===");
                LogClient("=======================================================");
            }
            catch (Exception ex)
            {
                LogClient($"[CLIENT] ERROR in LoadScoresData: {ex.Message}");
                LogClient($"[CLIENT] Stack trace: {ex.StackTrace}");
                LogClient("=======================================================");
                MessageBox.Show("Lỗi tải điểm: " + ex.Message + "\n\n" + ex.StackTrace, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowLoginDialog()
        {
            Form loginForm = new Form
            {
                Text = "Đăng nhập",
                Size = new Size(400, 250),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblUsername = new Label
            {
                Text = "Mã sinh viên:",
                Location = new Point(30, 30),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            loginForm.Controls.Add(lblUsername);

            TextBox txtUsername = new TextBox
            {
                Location = new Point(30, 55),
                Size = new Size(320, 28),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            loginForm.Controls.Add(txtUsername);

            Label lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(30, 95),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            loginForm.Controls.Add(lblPassword);

            TextBox txtPassword = new TextBox
            {
                Location = new Point(30, 120),
                Size = new Size(320, 28),
                PasswordChar = '●',
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            loginForm.Controls.Add(txtPassword);

            Button btnLogin = CreateModernButton("Đăng nhập", ThemeAccent, Color.White, 150, 38);
            btnLogin.Location = new Point(30, 165);
            btnLogin.Font = new Font("Segoe UI Semibold", 11);
            btnLogin.Click += (s, ev) =>
            {
                if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo");
                    return;
                }

                // For demo: accept any username/password
                SessionManager.Login(txtUsername.Text, txtPassword.Text);
                MessageBox.Show("Đăng nhập thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                loginForm.Close();
            };
            loginForm.Controls.Add(btnLogin);

            Button btnCancel = CreateModernButton("Hủy", Color.FromArgb(70, 73, 95), ThemeTextSecondary, 100, 38);
            btnCancel.Location = new Point(190, 165);
            btnCancel.Click += (s, ev) => loginForm.Close();
            loginForm.Controls.Add(btnCancel);

            loginForm.ShowDialog();
            
            // Auto-load scores after successful login
            if (SessionManager.IsLoggedIn)
            {
                _ = Task.Run(async () => await LoadScoresData());
            }
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            if (dgvScores.DataSource == null) return;

            foreach (DataGridViewRow row in dgvScores.Rows)
            {
                if (row.Cells.Count == 0) continue;
                
                try
                {
                    // Chọn/bỏ chọn tất cả môn học (bao gồm cả thể chất)
                    row.Cells["Chọn"].Value = isSelectAllMode;
                }
                catch
                {
                    continue;
                }
            }

            // Toggle mode and update button text
            isSelectAllMode = !isSelectAllMode;
            btnSelectAll.Text = isSelectAllMode ? "☑ Chọn tất cả" : "☐ Bỏ chọn tất cả";

            CalculatePredictedCPA();
        }

        private void DgvScores_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvScores.Rows[e.RowIndex];
            var columnName = dgvScores.Columns[e.ColumnIndex].Name;

            // Auto-calculate when scores change
            if (columnName == "Điểm_CC" || columnName == "Điểm_GK" || columnName == "Điểm_CK")
            {
                try
                {
                    float cc = Convert.ToSingle(row.Cells["Điểm_CC"].Value ?? 0);
                    float gk = Convert.ToSingle(row.Cells["Điểm_GK"].Value ?? 0);
                    float ck = Convert.ToSingle(row.Cells["Điểm_CK"].Value ?? 0);

                    float overall = CalculateOverallScore(cc, gk, ck);
                    string text = ConvertScoreToText(overall);

                    row.Cells["Điểm_TK"].Value = overall;
                    row.Cells["Điểm_chữ"].Value = text;
                    row.Cells["Trạng_thái"].Value = overall >= 4.0f ? "✓" : "✗";
                    row.Cells["IsPassed"].Value = overall >= 4.0f;

                    // Update IsPE
                    string subjectName = row.Cells["Môn_học"].Value?.ToString() ?? "";
                    row.Cells["IsPE"].Value = IsPhysicalEducation(subjectName);

                    CalculatePredictedCPA();
                }
                catch { }
            }
            
            // Recalculate when any other field changes
            if (columnName == "Tín_chỉ" || columnName == "Môn_học")
            {
                CalculatePredictedCPA();
            }
        }

        private void DgvScores_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (dgvScores.Columns[e.ColumnIndex].Name == "Chọn")
                {
                    CalculatePredictedCPA();
                }
            }
        }

        private void DgvScores_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (dgvScores.Columns[e.ColumnIndex].Name == "Chọn")
                {
                    // Commit the edit immediately for checkbox
                    dgvScores.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    CalculatePredictedCPA();
                }
            }
        }

        private void CalculatePredictedCPA()
        {
            if (dgvScores.DataSource == null) return;

            double totalGradePoints = 0;
            long totalCredits = 0;
            long selectedCredits = 0; // Tổng tín chỉ đã chọn

            foreach (DataGridViewRow row in dgvScores.Rows)
            {
                if (row.Cells.Count == 0) continue;
                
                try
                {
                    bool isSelected = row.Cells["Chọn"].Value != null && (bool)row.Cells["Chọn"].Value;
                    bool isPassed = row.Cells["IsPassed"].Value != null && (bool)row.Cells["IsPassed"].Value;
                    bool isPE = row.Cells["IsPE"].Value != null && (bool)row.Cells["IsPE"].Value;

                    if (isSelected)
                    {
                        long credits = Convert.ToInt64(row.Cells["Tín_chỉ"].Value);
                        selectedCredits += credits;

                        // Only add to CPA calculation if passed and not PE
                        if (isPassed && !isPE)
                        {
                            float scoreOverall = Convert.ToSingle(row.Cells["Điểm_TK"].Value);
                            double gradePoint = ConvertScoreToGradePoint(scoreOverall);
                            totalGradePoints += gradePoint * credits;
                            totalCredits += credits;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            double predictedCPA = totalCredits > 0 ? totalGradePoints / totalCredits : 0;
            lblPredictedCPA.Text = $"CPA DỰ KIẾN: {predictedCPA:F2}";
            lblTotalCredits.Text = $"Tổng tín chỉ đã chọn: {selectedCredits}";

            // Color based on CPA
            if (predictedCPA >= 3.6)
                lblPredictedCPA.ForeColor = Color.FromArgb(102, 187, 106); // Green
            else if (predictedCPA >= 3.0)
                lblPredictedCPA.ForeColor = Color.FromArgb(255, 215, 0); // Gold
            else if (predictedCPA >= 2.5)
                lblPredictedCPA.ForeColor = Color.FromArgb(255, 183, 77); // Orange
            else
                lblPredictedCPA.ForeColor = Color.FromArgb(239, 83, 80); // Red
        }

        private double ConvertScoreToGradePoint(float score)
        {
            if (score >= 9.0) return 4.0;   // A+
            if (score >= 8.5) return 3.8;   // A
            if (score >= 7.8) return 3.5;   // B+
            if (score >= 7.0) return 3.0;   // B
            if (score >= 6.3) return 2.4;   // C+
            if (score >= 5.5) return 2.0;   // C
            if (score >= 4.8) return 1.5;   // D+
            if (score >= 4.0) return 1.0;   // D
            return 0.0;                     // F
        }

        private string ConvertScoreToText(float score)
        {
            if (score >= 9.0) return "A+";
            if (score >= 8.5) return "A";
            if (score >= 7.8) return "B+";
            if (score >= 7.0) return "B";
            if (score >= 6.3) return "C+";
            if (score >= 5.5) return "C";
            if (score >= 4.8) return "D+";
            if (score >= 4.0) return "D";
            return "F";
        }

        private float CalculateOverallScore(float cc, float gk, float ck)
        {
            // Công thức: (0.3*CC + 0.7*GK)*0.3 + 0.7*CK
            float result = (0.3f * cc + 0.7f * gk) * 0.3f + 0.7f * ck;
            return (float)Math.Round(result, 2);
        }

        private bool IsPhysicalEducation(string subjectName)
        {
            string[] peKeywords = { "thể chất", "thể dục", "giáo dục thể chất", "gdtc", "physical education", "pe" };
            string lowerName = subjectName.ToLower();
            return peKeywords.Any(keyword => lowerName.Contains(keyword));
        }

        // Masking methods
        private string MaskStudentCode(string studentCode)
        {
            if (string.IsNullOrEmpty(studentCode) || studentCode.Length < 4)
                return studentCode;
            
            // Mask middle characters, keep first 2 and last 2
            string masked = studentCode.Substring(0, 2);
            for (int i = 2; i < studentCode.Length - 2; i++)
            {
                masked += "*";
            }
            if (studentCode.Length > 2)
                masked += studentCode.Substring(studentCode.Length - 2);
            
            return masked;
        }

        private string MaskClass(string className)
        {
            if (string.IsNullOrEmpty(className) || className.Length < 3)
                return className;
            
            // Mask middle characters, keep first and last
            string masked = className.Substring(0, 1);
            for (int i = 1; i < className.Length - 1; i++)
            {
                masked += "*";
            }
            masked += className.Substring(className.Length - 1);
            
            return masked;
        }

        private void BtnAddSubject_Click(object sender, EventArgs e)
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Add new row to DataTable
            var currentList = dgvScores.DataSource as List<VirtualScoreItem>;
            if (currentList == null)
            {
                currentList = new List<VirtualScoreItem>();
            }

            var newScore = new VirtualScoreItem
            {
                Chọn = true,
                Môn_học = "Nhập tên môn học...",
                Tín_chỉ = 3,
                Điểm_CC = 0,
                Điểm_GK = 0,
                Điểm_CK = 0,
                Điểm_TK = 0,
                Điểm_chữ = "F",
                Trạng_thái = "✗",
                IsPassed = false,
                IsCurrentSemester = false,
                IsPE = false
            };

            currentList.Add(newScore);
            
            // Refresh DataGridView
            dgvScores.DataSource = null;
            dgvScores.DataSource = currentList;

            // Reconfigure columns
            ConfigureDataGridColumns();

            // Scroll to new row
            if (dgvScores.Rows.Count > 0)
            {
                dgvScores.FirstDisplayedScrollingRowIndex = dgvScores.Rows.Count - 1;
                dgvScores.Rows[dgvScores.Rows.Count - 1].Selected = true;
            }
        }

        private void BtnDeleteSubject_Click(object sender, EventArgs e)
        {
            if (dgvScores.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn môn học cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa môn học đã chọn?", "Xác nhận xóa", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            try
            {
                var currentList = dgvScores.DataSource as List<VirtualScoreItem>;
                if (currentList == null) return;

                // Get selected indices (reverse order to avoid index shifting)
                var selectedIndices = dgvScores.SelectedRows.Cast<DataGridViewRow>()
                    .Select(row => row.Index)
                    .OrderByDescending(i => i)
                    .ToList();

                // Remove items
                foreach (int index in selectedIndices)
                {
                    if (index >= 0 && index < currentList.Count)
                    {
                        currentList.RemoveAt(index);
                    }
                }

                // Refresh DataGridView
                dgvScores.DataSource = null;
                dgvScores.DataSource = currentList;

                // Reconfigure columns
                ConfigureDataGridColumns();

                // Recalculate CPA
                CalculatePredictedCPA();

                MessageBox.Show($"Đã xóa {selectedIndices.Count} môn học!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureDataGridColumns()
        {
            if (dgvScores.Columns.Contains("Chọn"))
            {
                dgvScores.Columns["Chọn"].ReadOnly = false;
                dgvScores.Columns["Chọn"].Width = 50;
            }

            // Editable columns
            if (dgvScores.Columns.Contains("Môn_học"))
                dgvScores.Columns["Môn_học"].ReadOnly = false;
            if (dgvScores.Columns.Contains("Tín_chỉ"))
                dgvScores.Columns["Tín_chỉ"].ReadOnly = false;
            if (dgvScores.Columns.Contains("Điểm_CC"))
                dgvScores.Columns["Điểm_CC"].ReadOnly = false;
            if (dgvScores.Columns.Contains("Điểm_GK"))
                dgvScores.Columns["Điểm_GK"].ReadOnly = false;
            if (dgvScores.Columns.Contains("Điểm_CK"))
                dgvScores.Columns["Điểm_CK"].ReadOnly = false;

            // Hide helper columns
            if (dgvScores.Columns.Contains("IsPassed"))
                dgvScores.Columns["IsPassed"].Visible = false;
            if (dgvScores.Columns.Contains("IsCurrentSemester"))
                dgvScores.Columns["IsCurrentSemester"].Visible = false;
            if (dgvScores.Columns.Contains("IsPE"))
                dgvScores.Columns["IsPE"].Visible = false;

            // Readonly calculated columns
            if (dgvScores.Columns.Contains("Điểm_TK"))
                dgvScores.Columns["Điểm_TK"].ReadOnly = true;
            if (dgvScores.Columns.Contains("Điểm_chữ"))
                dgvScores.Columns["Điểm_chữ"].ReadOnly = true;
            if (dgvScores.Columns.Contains("Trạng_thái"))
                dgvScores.Columns["Trạng_thái"].ReadOnly = true;
        }

        private async void BtnSaveToServer_Click(object sender, EventArgs e)
        {
            if (dgvScores.DataSource == null)
            {
                MessageBox.Show("Chưa có dữ liệu để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (serverKeyPair == null)
            {
                MessageBox.Show("Lỗi: Không có server key pair!\n\nVui lòng khởi động server từ form chính trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                btnSaveToServer.Enabled = false;
                btnSaveToServer.Text = "⏳ Đang lưu...";

                // Get all scores from DataTable
                var allScores = dgvScores.DataSource as List<VirtualScoreItem>;
                if (allScores == null || allScores.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Send save request to server via encrypted channel
                string studentCode = SessionManager.Username;
                bool success = await SendSaveScoresRequestAsync(studentCode, allScores);

                if (success)
                {
                    MessageBox.Show($"Đã lưu {allScores.Count} môn học vào database!\n\nCPA dự kiến: {lblPredictedCPA.Text}", 
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Lỗi khi lưu dữ liệu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveToServer.Enabled = true;
                btnSaveToServer.Text = "💾 Lưu vào DB";
            }
        }

        private async Task<bool> SendSaveScoresRequestAsync(string studentCode, List<VirtualScoreItem> scores)
        {
            try
            {
                TransmissionLogger.LogClient("=======================================================");
                TransmissionLogger.LogClient($"[CLIENT] Save Virtual Scores Request - Student Code: {studentCode}");

                using (var client = new System.Net.Sockets.TcpClient())
                {
                    TransmissionLogger.LogClient($"[CLIENT] Connecting to Server 127.0.0.1:8888...");
                    await client.ConnectAsync("127.0.0.1", 8888);
                    TransmissionLogger.LogClient("[CLIENT] Connected successfully!");

                    var stream = client.GetStream();

                    // 1. Create random AES key
                    var aesKey = new AESKey();
                    TransmissionLogger.LogClient($"[CLIENT] Generated AES Key");

                    // 2. Encrypt AES key with RSA
                    var rsa = new Crypto.RSA(serverKeyPair.N, serverKeyPair.E);
                    byte[] encryptedAESKey = rsa.Encrypt(aesKey.Key);
                    TransmissionLogger.LogClient($"[CLIENT] Encrypted AES Key with RSA ({encryptedAESKey.Length} bytes)");

                    // 3. Send encrypted AES key
                    byte[] keyLengthBytes = BitConverter.GetBytes(encryptedAESKey.Length);
                    await stream.WriteAsync(keyLengthBytes, 0, 4);
                    await stream.WriteAsync(encryptedAESKey, 0, encryptedAESKey.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Sent Encrypted AES Key");

                    // 4. Send IV
                    await stream.WriteAsync(aesKey.IV, 0, aesKey.IV.Length);
                    TransmissionLogger.LogClient($"[CLIENT] Sent IV");

                    // Initialize AES cipher for encryption/decryption
                    var aes = new Crypto.AES(aesKey.Key);

                    // 5. Create request
                    try
                    {
                        TransmissionLogger.LogClient($"[CLIENT] === CREATING REQUEST ===");
                        var saveRequest = new
                        {
                            action = "save_virtual_scores",
                            studentCode = studentCode,
                            scores = scores.Select(s => new
                            {
                                subjectName = s.Môn_học,
                                subjectCredit = s.Tín_chỉ,
                                scoreFirst = s.Điểm_CC,
                                scoreSecond = s.Điểm_GK,
                                scoreFinal = s.Điểm_CK,
                                scoreOverall = s.Điểm_TK,
                                scoreText = s.Điểm_chữ,
                                isSelected = s.Chọn
                            }).ToList()
                        };
                        TransmissionLogger.LogClient($"[CLIENT] Request object created successfully");

                        string requestJson = System.Text.Json.JsonSerializer.Serialize(saveRequest, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            WriteIndented = false
                        });
                        TransmissionLogger.LogClient($"[CLIENT] JSON serialization completed");
                        
                        // Log detailed request info
                        TransmissionLogger.LogClient($"[CLIENT] === REQUEST DETAILS ===");
                        TransmissionLogger.LogClient($"[CLIENT] Action: {saveRequest.action}");
                        TransmissionLogger.LogClient($"[CLIENT] Student Code: {saveRequest.studentCode}");
                        TransmissionLogger.LogClient($"[CLIENT] Total Scores: {saveRequest.scores.Count()}");
                        
                        // Log selected scores summary
                        var selectedScores = saveRequest.scores.Where(s => s.isSelected).ToList();
                        TransmissionLogger.LogClient($"[CLIENT] Selected Scores: {selectedScores.Count}");
                        
                        // Log first few selected scores for verification
                        int logCount = Math.Min(3, selectedScores.Count);
                        for (int i = 0; i < logCount; i++)
                        {
                            var score = selectedScores[i];
                            TransmissionLogger.LogClient($"[CLIENT] Score {i+1}: {score.subjectName} - TC:{score.subjectCredit} - TK:{score.scoreOverall} - Selected:{score.isSelected}");
                        }
                        if (selectedScores.Count > 3)
                        {
                            TransmissionLogger.LogClient($"[CLIENT] ... and {selectedScores.Count - 3} more selected scores");
                        }
                        
                        TransmissionLogger.LogClient($"[CLIENT] Request JSON Length: {requestJson.Length} chars");
                        TransmissionLogger.LogClient($"[CLIENT] Request JSON Preview: {requestJson.Substring(0, Math.Min(300, requestJson.Length))}...");
                        TransmissionLogger.LogClient($"[CLIENT] === END REQUEST DETAILS ===");

                        // 6. Encrypt request
                        TransmissionLogger.LogClient($"[CLIENT] === STARTING ENCRYPTION ===");
                        TransmissionLogger.LogClient($"[CLIENT] AES cipher initialized");
                        
                        byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(requestJson);
                        TransmissionLogger.LogClient($"[CLIENT] Request converted to bytes: {requestBytes.Length} bytes");
                        
                        byte[] encryptedRequest = aes.EncryptCBC(requestBytes, aesKey.IV);
                        TransmissionLogger.LogClient($"[CLIENT] === ENCRYPTION DETAILS ===");
                        TransmissionLogger.LogClient($"[CLIENT] Original data size: {requestBytes.Length} bytes");
                        TransmissionLogger.LogClient($"[CLIENT] Encrypted data size: {encryptedRequest.Length} bytes");
                        TransmissionLogger.LogClient($"[CLIENT] Encryption ratio: {(double)encryptedRequest.Length / requestBytes.Length:F2}x");
                        TransmissionLogger.LogClient($"[CLIENT] === END ENCRYPTION DETAILS ===");

                        // 7. Send encrypted request
                        TransmissionLogger.LogClient($"[CLIENT] === STARTING TRANSMISSION ===");
                        byte[] requestLengthBytes = BitConverter.GetBytes(encryptedRequest.Length);
                        TransmissionLogger.LogClient($"[CLIENT] Sending request length header: {encryptedRequest.Length} bytes");
                        
                        await stream.WriteAsync(requestLengthBytes, 0, 4);
                        TransmissionLogger.LogClient($"[CLIENT] Request length header sent successfully");
                        
                        await stream.WriteAsync(encryptedRequest, 0, encryptedRequest.Length);
                        TransmissionLogger.LogClient($"[CLIENT] === TRANSMISSION ===");
                        TransmissionLogger.LogClient($"[CLIENT] Sent request length header: {encryptedRequest.Length} bytes");
                        TransmissionLogger.LogClient($"[CLIENT] Sent encrypted request payload: {encryptedRequest.Length} bytes");
                        TransmissionLogger.LogClient($"[CLIENT] Total transmitted: {4 + encryptedRequest.Length} bytes");
                        TransmissionLogger.LogClient($"[CLIENT] === END TRANSMISSION ===");
                    }
                    catch (Exception ex)
                    {
                        TransmissionLogger.LogClient($"[CLIENT] ERROR in request creation/encryption: {ex.Message}");
                        TransmissionLogger.LogClient($"[CLIENT] Stack trace: {ex.StackTrace}");
                        throw;
                    }

                    // 8. Receive response
                    TransmissionLogger.LogClient("[CLIENT] === RECEIVING RESPONSE ===");
                    TransmissionLogger.LogClient("[CLIENT] Waiting for response...");
                    byte[] responseLengthBytes = new byte[4];
                    await stream.ReadAsync(responseLengthBytes, 0, 4);
                    int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                    TransmissionLogger.LogClient($"[CLIENT] Response length header: {responseLength} bytes");

                    if (responseLength <= 0)
                    {
                        TransmissionLogger.LogClient($"[CLIENT] ERROR: Received empty response");
                        TransmissionLogger.LogClient("=======================================================");
                        return false;
                    }

                    byte[] encryptedResponse = new byte[responseLength];
                    int totalRead = 0;
                    while (totalRead < responseLength)
                    {
                        int read = await stream.ReadAsync(encryptedResponse, totalRead, responseLength - totalRead);
                        totalRead += read;
                        TransmissionLogger.LogClient($"[CLIENT] Read {read} bytes, total: {totalRead}/{responseLength}");
                    }
                    TransmissionLogger.LogClient($"[CLIENT] Received complete encrypted response: {responseLength} bytes");

                    // 9. Decrypt response
                    byte[] decryptedResponse = aes.DecryptCBC(encryptedResponse, aesKey.IV);
                    string responseJson = System.Text.Encoding.UTF8.GetString(decryptedResponse);
                    TransmissionLogger.LogClient($"[CLIENT] === RESPONSE DETAILS ===");
                    TransmissionLogger.LogClient($"[CLIENT] Decrypted response size: {decryptedResponse.Length} bytes");
                    TransmissionLogger.LogClient($"[CLIENT] Response JSON: {responseJson}");

                    var response = System.Text.Json.JsonSerializer.Deserialize<dynamic>(responseJson);
                    bool success = response.GetProperty("success").GetBoolean();
                    string message = response.GetProperty("message").GetString() ?? "";
                    
                    TransmissionLogger.LogClient($"[CLIENT] Response Success: {success}");
                    TransmissionLogger.LogClient($"[CLIENT] Response Message: {message}");
                    TransmissionLogger.LogClient($"[CLIENT] === END RESPONSE DETAILS ===");
                    TransmissionLogger.LogClient("=======================================================");

                    return success;
                }
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogClient($"[CLIENT] Save error: {ex.Message}");
                TransmissionLogger.LogClient("=======================================================");
                return false;
            }
        }


        private void BtnGradeTable_Click(object sender, EventArgs e)
        {
            Form gradeTableForm = new Form
            {
                Text = "📊 Bảng quy đổi điểm",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            DataGridView dgvGradeTable = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(540, 420),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            StyleDataGridView(dgvGradeTable);

            var gradeData = new[]
            {
                new { Thang_10 = "9.0 - 10.0", Thang_4 = "4.0", Điểm_chữ = "A+", Xếp_loại = "Xuất sắc" },
                new { Thang_10 = "8.5 - 8.9", Thang_4 = "3.8", Điểm_chữ = "A", Xếp_loại = "Giỏi" },
                new { Thang_10 = "7.8 - 8.4", Thang_4 = "3.5", Điểm_chữ = "B+", Xếp_loại = "Khá" },
                new { Thang_10 = "7.0 - 7.7", Thang_4 = "3.0", Điểm_chữ = "B", Xếp_loại = "Khá" },
                new { Thang_10 = "6.3 - 6.9", Thang_4 = "2.4", Điểm_chữ = "C+", Xếp_loại = "Trung bình" },
                new { Thang_10 = "5.5 - 6.2", Thang_4 = "2.0", Điểm_chữ = "C", Xếp_loại = "Trung bình" },
                new { Thang_10 = "4.8 - 5.4", Thang_4 = "1.5", Điểm_chữ = "D+", Xếp_loại = "Trung bình yếu" },
                new { Thang_10 = "4.0 - 4.7", Thang_4 = "1.0", Điểm_chữ = "D", Xếp_loại = "Trung bình yếu" },
                new { Thang_10 = "0.0 - 3.9", Thang_4 = "0.0", Điểm_chữ = "F", Xếp_loại = "Kém" }
            };

            dgvGradeTable.DataSource = gradeData;
            gradeTableForm.Controls.Add(dgvGradeTable);
            gradeTableForm.ShowDialog();
        }

        private void BtnCPACalculator_Click(object sender, EventArgs e)
        {
            Form cpaForm = new Form
            {
                Text = "🎯 Tính toán CPA mục tiêu",
                Size = new Size(700, 700),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int y = 20;

            Label lblTitle = new Label
            {
                Text = "TÍNH TOÁN CPA MỤC TIÊU",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0)
            };
            cpaForm.Controls.Add(lblTitle);
            y += 50;

            // Current CPA
            Label lblCurrentCPA = new Label
            {
                Text = "CPA hiện tại:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            cpaForm.Controls.Add(lblCurrentCPA);

            TextBox txtCurrentCPA = new TextBox
            {
                Location = new Point(200, y - 3),
                Size = new Size(100, 28),
                Text = lblPredictedCPA.Text.Contains(":") ? lblPredictedCPA.Text.Split(':')[1].Trim() : "0.00",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            cpaForm.Controls.Add(txtCurrentCPA);
            y += 40;

            // Current credits
            Label lblCurrentCredits = new Label
            {
                Text = "Tín chỉ đã hoàn thành:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            cpaForm.Controls.Add(lblCurrentCredits);

            TextBox txtCurrentCredits = new TextBox
            {
                Location = new Point(200, y - 3),
                Size = new Size(100, 28),
                Text = lblTotalCredits.Text.Contains(":") ? lblTotalCredits.Text.Split(':')[1].Trim() : "0",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            cpaForm.Controls.Add(txtCurrentCredits);
            y += 40;

            // Target CPA
            Label lblTargetCPA = new Label
            {
                Text = "CPA mục tiêu:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeWarn
            };
            cpaForm.Controls.Add(lblTargetCPA);

            TextBox txtTargetCPA = new TextBox
            {
                Location = new Point(200, y - 3),
                Size = new Size(100, 28),
                Text = "3.00",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            cpaForm.Controls.Add(txtTargetCPA);
            y += 40;

            // Total credits to graduate
            Label lblTotalCreditsGrad = new Label
            {
                Text = "Tổng tín chỉ ra trường:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            cpaForm.Controls.Add(lblTotalCreditsGrad);

            TextBox txtTotalCreditsGrad = new TextBox
            {
                Location = new Point(200, y - 3),
                Size = new Size(100, 28),
                Text = "120",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            cpaForm.Controls.Add(txtTotalCreditsGrad);
            y += 50;

            // Grade distribution sliders
            Label lblDistribution = new Label
            {
                Text = "Phân bố điểm dự kiến (%):",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = Color.FromArgb(255, 215, 0)
            };
            cpaForm.Controls.Add(lblDistribution);
            y += 35;

            var gradeSliders = new Dictionary<string, (TrackBar? slider, Label? label, double gradePoint)>
            {
                {"A+ (4.0)", (null, null, 4.0)},
                {"A (3.8)", (null, null, 3.8)},
                {"B+ (3.5)", (null, null, 3.5)},
                {"B (3.0)", (null, null, 3.0)},
                {"C+ (2.4)", (null, null, 2.4)},
                {"C (2.0)", (null, null, 2.0)}
            };

            foreach (var grade in gradeSliders.Keys.ToList())
            {
                Label lblGrade = new Label
                {
                    Text = grade,
                    Location = new Point(30, y + 5),
                    Size = new Size(80, 20),
                    Font = new Font("Segoe UI", 9),
                    ForeColor = ThemeTextSecondary
                };
                cpaForm.Controls.Add(lblGrade);

                TrackBar slider = new TrackBar
                {
                    Location = new Point(120, y),
                    Size = new Size(400, 45),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    TickFrequency = 10
                };
                cpaForm.Controls.Add(slider);

                Label lblValue = new Label
                {
                    Text = "0%",
                    Location = new Point(530, y + 5),
                    Size = new Size(50, 20),
                    Font = new Font("Segoe UI Semibold", 9),
                    ForeColor = Color.FromArgb(76, 175, 80)
                };
                cpaForm.Controls.Add(lblValue);

                slider.ValueChanged += (s, ev) => lblValue.Text = $"{slider.Value}%";

                gradeSliders[grade] = (slider, lblValue, gradeSliders[grade].gradePoint);
                y += 50;
            }

            // Calculate button
            Button btnCalculate = CreateModernButton("🎯 Tính toán", Color.FromArgb(255, 87, 34), Color.White, 150, 38);
            btnCalculate.Location = new Point(20, y);
            btnCalculate.Font = new Font("Segoe UI Semibold", 11);
            btnCalculate.Click += (s, ev) =>
            {
                try
                {
                    double currentCPA = double.Parse(txtCurrentCPA.Text);
                    int currentCredits = int.Parse(txtCurrentCredits.Text);
                    double targetCPA = double.Parse(txtTargetCPA.Text);
                    int totalCreditsGrad = int.Parse(txtTotalCreditsGrad.Text);

                    int remainingCredits = totalCreditsGrad - currentCredits;
                    
                    if (remainingCredits <= 0)
                    {
                        MessageBox.Show("Bạn đã hoàn thành đủ tín chỉ!", "Thông báo");
                        return;
                    }

                    // Calculate weighted average from sliders
                    double totalPercent = gradeSliders.Values.Sum(v => v.slider?.Value ?? 0);
                    if (totalPercent == 0)
                    {
                        MessageBox.Show("Vui lòng điều chỉnh phân bố điểm!", "Thông báo");
                        return;
                    }

                    double weightedGPA = 0;
                    foreach (var grade in gradeSliders.Values)
                    {
                        if (grade.slider != null)
                        {
                            weightedGPA += (grade.slider.Value / totalPercent) * grade.gradePoint;
                        }
                    }

                    // Calculate final CPA
                    double totalPoints = (currentCPA * currentCredits) + (weightedGPA * remainingCredits);
                    double finalCPA = totalPoints / totalCreditsGrad;

                    // Calculate credits needed by grade
                    string result = $"KẾT QUẢ TÍNH TOÁN:\n\n";
                    result += $"CPA hiện tại: {currentCPA:F2}\n";
                    result += $"Tín chỉ còn lại: {remainingCredits}\n";
                    result += $"GPA trung bình cần đạt: {weightedGPA:F2}\n\n";
                    result += $"CPA dự kiến khi tốt nghiệp: {finalCPA:F2}\n\n";

                    if (finalCPA >= targetCPA)
                    {
                        result += $"✓ ĐẠT mục tiêu CPA {targetCPA:F2}!\n\n";
                    }
                    else
                    {
                        result += $"✗ CHƯA ĐẠT mục tiêu CPA {targetCPA:F2}\n";
                        double neededGPA = (targetCPA * totalCreditsGrad - currentCPA * currentCredits) / remainingCredits;
                        result += $"GPA cần đạt: {neededGPA:F2}\n\n";
                    }

                    result += "PHÂN BỐ TÍN CHỈ DỰ KIẾN:\n";
                    foreach (var grade in gradeSliders)
                    {
                        if (grade.Value.slider != null && grade.Value.slider.Value > 0)
                        {
                            int credits = (int)(remainingCredits * grade.Value.slider.Value / totalPercent);
                            result += $"{grade.Key}: {credits} tín chỉ ({grade.Value.slider.Value}%)\n";
                        }
                    }

                    MessageBox.Show(result, "Kết quả tính toán CPA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            cpaForm.Controls.Add(btnCalculate);

            cpaForm.ShowDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            TransmissionLogger.OnClientLog -= LogClient;
            base.OnFormClosing(e);
        }
    }

    public class VirtualScoreItem
    {
        public bool Chọn { get; set; }
        public string Môn_học { get; set; }
        public long Tín_chỉ { get; set; }
        public float Điểm_CC { get; set; }
        public float Điểm_GK { get; set; }
        public float Điểm_CK { get; set; }
        public float Điểm_TK { get; set; }
        public string Điểm_chữ { get; set; }
        public string Trạng_thái { get; set; }
        public bool IsPassed { get; set; }
        public bool IsCurrentSemester { get; set; }
        public bool IsPE { get; set; }
    }
}
