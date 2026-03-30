using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using DataMasking.Database;
using DataMasking.Masking;
using DataMasking.Models;
using DataMasking.Network;
using DataMasking.Key;

namespace DataMasking
{
    public class ScoreLookupForm : Form
    {
        private StudentDatabaseManager dbManager;
        private MaskingService maskingService;
        private StudentClientService clientService;
        private StudentServerService serverService;
        private RSAKeyPair serverKeyPair;
        private bool isServerRunning = false;
        
        private TextBox txtStudentCode;
        private Button btnSearch;
        private Label lblStudentInfo;
        private DataGridView dgvScores;
        private Label lblGPA;
        private Label lblCPA;
        private Label lblTotalCredits;
        private ComboBox cboMaskingType;
        private Button btnStartServer;
        private Button btnStopServer;
        private Button btnLogout; // Logout button reference
        private Label lblServerStatus;
        private TextBox txtClientLog;
        private TextBox txtServerLog;
        private Form serverLogForm;
        
        // Theme colors
        private static readonly Color ThemeBg = Color.FromArgb(30, 30, 46);
        private static readonly Color ThemePanel = Color.FromArgb(40, 42, 58);
        private static readonly Color ThemeHeader = Color.FromArgb(24, 24, 37);
        private static readonly Color ThemeAccent = Color.FromArgb(0, 150, 136);
        private static readonly Color ThemeAccentHover = Color.FromArgb(0, 180, 160);
        private static readonly Color ThemeTextPrimary = Color.FromArgb(230, 230, 240);
        private static readonly Color ThemeTextSecondary = Color.FromArgb(160, 165, 185);
        private static readonly Color ThemeInput = Color.FromArgb(50, 52, 70);
        private static readonly Color ThemeInputBorder = Color.FromArgb(70, 75, 95);
        private static readonly Color ThemeSuccess = Color.FromArgb(102, 187, 106);
        private static readonly Color ThemeWarn = Color.FromArgb(255, 183, 77);

        public ScoreLookupForm()
        {
            dbManager = new StudentDatabaseManager("36.50.54.109", "kmalegend", "anonymous", "Thuanld@255", 3306);
            maskingService = new MaskingService();
            
            // Tạo RSA key pair cho server
            serverKeyPair = new RSAKeyPair(1024);
            
            // Khởi tạo services
            clientService = new StudentClientService(serverKeyPair, "127.0.0.1", 8888);
            serverService = new StudentServerService(serverKeyPair, dbManager);
            
            InitializeComponents();
            
            // Subscribe to logs
            TransmissionLogger.OnClientLog += LogClient;
            TransmissionLogger.OnServerLog += LogServer;
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

        private void LogServer(string message)
        {
            if (txtServerLog != null && txtServerLog.InvokeRequired)
            {
                try { txtServerLog.Invoke(new Action<string>(LogServer), message); } catch { }
            }
            else if (txtServerLog != null)
            {
                txtServerLog.AppendText(message + Environment.NewLine);
            }
        }

        private void InitializeComponents()
        {
            this.Text = "HỆ THỐNG QUẢN LÝ HỌC TẬP TOÀN DIỆN KMA LEGEND";
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
                Text = "HỆ THỐNG QUẢN LÝ HỌC TẬP TOÀN DIỆN KMA LEGEND",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Tra cứu điểm thi online với Data Masking • RSA + AES Encryption",
                Location = new Point(20, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = ThemeTextSecondary
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Server controls
            btnStartServer = CreateModernButton("▶ START SERVER", ThemeSuccess, Color.White, 140, 32);
            btnStartServer.Location = new Point(900, 14);
            btnStartServer.Click += BtnStartServer_Click;
            pnlHeader.Controls.Add(btnStartServer);

            btnStopServer = CreateModernButton("⏹ STOP SERVER", Color.FromArgb(239, 83, 80), Color.White, 140, 32);
            btnStopServer.Location = new Point(1050, 14);
            btnStopServer.Enabled = false;
            btnStopServer.Click += BtnStopServer_Click;
            pnlHeader.Controls.Add(btnStopServer);

            lblServerStatus = new Label
            {
                Text = "● STOPPED",
                Location = new Point(1200, 22),
                AutoSize = true,
                ForeColor = Color.FromArgb(239, 83, 80),
                Font = new Font("Segoe UI Semibold", 10)
            };
            pnlHeader.Controls.Add(lblServerStatus);

            // Search Panel
            Panel pnlSearch = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(900, 80),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlSearch);

            Label lblCode = new Label
            {
                Text = "Mã sinh viên:",
                Location = new Point(20, 15),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            pnlSearch.Controls.Add(lblCode);

            txtStudentCode = new TextBox
            {
                Location = new Point(20, 40),
                Size = new Size(250, 28),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };
            pnlSearch.Controls.Add(txtStudentCode);

            Label lblMaskType = new Label
            {
                Text = "Phương thức Masking:",
                Location = new Point(290, 15),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeWarn
            };
            pnlSearch.Controls.Add(lblMaskType);

            cboMaskingType = new ComboBox
            {
                Location = new Point(290, 40),
                Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary
            };
            cboMaskingType.Items.AddRange(new object[]
            {
                "Che mặt nạ ký tự",
                "Xáo trộn dữ liệu",
                "Thay thế bằng dữ liệu giả"
            });
            cboMaskingType.SelectedIndex = 0;
            pnlSearch.Controls.Add(cboMaskingType);

            btnSearch = CreateModernButton("🔍  Tra cứu", ThemeAccent, Color.White, 150, 38);
            btnSearch.Location = new Point(560, 35);
            btnSearch.Font = new Font("Segoe UI Semibold", 11);
            btnSearch.Click += BtnSearch_Click;
            pnlSearch.Controls.Add(btnSearch);

            Button btnConfigDB = CreateModernButton("Config DB", Color.FromArgb(70, 73, 95), ThemeTextPrimary, 110, 38);
            btnConfigDB.Location = new Point(730, 35);
            btnConfigDB.Font = new Font("Segoe UI", 9);
            btnConfigDB.Click += BtnConfigDB_Click;
            pnlSearch.Controls.Add(btnConfigDB);

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
                Text = "📡 CLIENT TRANSMISSION LOG",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = Color.FromArgb(0, 230, 230)
            };
            pnlClientLog.Controls.Add(lblClientLog);

            Button btnClearClientLog = CreateModernButton("Xóa", Color.FromArgb(60, 63, 80), ThemeTextSecondary, 60, 24);
            btnClearClientLog.Location = new Point(550, 8);
            btnClearClientLog.Font = new Font("Segoe UI", 8);
            btnClearClientLog.Click += (s, e) => txtClientLog?.Clear();
            pnlClientLog.Controls.Add(btnClearClientLog);

            txtClientLog = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(600, 630),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 15, 25),
                ForeColor = Color.FromArgb(0, 230, 230),
                Font = new Font("Cascadia Code", 8.5f),
                BorderStyle = BorderStyle.None,
                WordWrap = false
            };
            pnlClientLog.Controls.Add(txtClientLog);

            // Student Info Panel
            Panel pnlInfo = new Panel
            {
                Location = new Point(20, 175),
                Size = new Size(900, 80),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlInfo);

            lblStudentInfo = new Label
            {
                Text = "Nhập mã sinh viên để tra cứu điểm...",
                Location = new Point(20, 15),
                Size = new Size(860, 50),
                Font = new Font("Segoe UI", 10),
                ForeColor = ThemeTextSecondary
            };
            pnlInfo.Controls.Add(lblStudentInfo);

            // Scores DataGridView
            dgvScores = new DataGridView
            {
                Location = new Point(20, 270),
                Size = new Size(900, 400),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            StyleDataGridView(dgvScores);
            dgvScores.CellDoubleClick += DgvScores_CellDoubleClick;
            this.Controls.Add(dgvScores);

            // Summary Panel - Redesigned with 2 columns
            Panel pnlSummary = new Panel
            {
                Location = new Point(20, 685),
                Size = new Size(900, 110),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlSummary);

            // Column 1: Stats (left side)
            int col1X = 20;
            lblGPA = new Label
            {
                Text = "GPA kỳ này: --",
                Location = new Point(col1X, 12),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeSuccess
            };
            pnlSummary.Controls.Add(lblGPA);

            lblCPA = new Label
            {
                Text = "CPA: --",
                Location = new Point(col1X, 35),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeAccentHover
            };
            pnlSummary.Controls.Add(lblCPA);

            lblTotalCredits = new Label
            {
                Text = "Tổng tín chỉ hoàn thành: --",
                Location = new Point(col1X, 58),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeWarn
            };
            pnlSummary.Controls.Add(lblTotalCredits);

            // Column 2: Buttons (right side)
            int col2X = 500;
            Button btnGradeTable = CreateModernButton("Bảng quy đổi", Color.FromArgb(70, 73, 95), ThemeTextPrimary, 120, 32);
            btnGradeTable.Location = new Point(col2X, 10);
            btnGradeTable.Click += BtnGradeTable_Click;
            pnlSummary.Controls.Add(btnGradeTable);

            Button btnStatistics = CreateModernButton("Thống kê", Color.FromArgb(70, 73, 95), ThemeTextPrimary, 120, 32);
            btnStatistics.Location = new Point(col2X + 130, 10);
            btnStatistics.Click += BtnStatistics_Click;
            pnlSummary.Controls.Add(btnStatistics);

            Button btnCPACalculator = CreateModernButton("CPA mục tiêu", Color.FromArgb(70, 73, 95), ThemeTextPrimary, 120, 32);
            btnCPACalculator.Location = new Point(col2X + 260, 10);
            btnCPACalculator.Click += BtnCPACalculator_Click;
            pnlSummary.Controls.Add(btnCPACalculator);

            // New buttons for ACTVN Portal
            Button btnViewTimetable = CreateModernButton("📅 Xem lịch học", Color.FromArgb(80, 60, 120), Color.FromArgb(200, 180, 255), 130, 32);
            btnViewTimetable.Location = new Point(col2X, 45);
            btnViewTimetable.Click += BtnViewTimetable_Click;
            pnlSummary.Controls.Add(btnViewTimetable);

            Button btnViewTranscript = CreateModernButton("📊 Bảng điểm ảo", Color.FromArgb(80, 60, 120), Color.FromArgb(200, 180, 255), 130, 32);
            btnViewTranscript.Location = new Point(col2X + 140, 45);
            btnViewTranscript.Click += BtnViewTranscript_Click;
            pnlSummary.Controls.Add(btnViewTranscript);

            // Logout button - only visible when logged in
            btnLogout = CreateModernButton("🚪 Đăng xuất", Color.FromArgb(183, 28, 28), Color.White, 110, 32);
            btnLogout.Location = new Point(col2X + 280, 45);
            btnLogout.Click += BtnLogout_Click;
            btnLogout.Visible = Utils.SessionManager.IsLoggedIn; // Initially hidden
            pnlSummary.Controls.Add(btnLogout);
        }

        private Button CreateModernButton(string text, Color bgColor, Color fgColor, int width, int height)
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
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 5, 5, 5);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 48, 65);
            dgv.RowTemplate.Height = 35;
        }

        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            string studentCode = txtStudentCode.Text.Trim();
            
            if (string.IsNullOrEmpty(studentCode))
            {
                MessageBox.Show("Vui lòng nhập mã sinh viên!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isServerRunning)
            {
                MessageBox.Show("Vui lòng khởi động Server trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnSearch.Enabled = false;
                btnSearch.Text = "⏳ Đang tra cứu...";

                // Send request to server via encrypted channel
                int maskingType = cboMaskingType.SelectedIndex;
                StudentScoreResponse response = await clientService.SendScoreLookupRequestAsync(studentCode, maskingType);

                if (response.Success)
                {
                    // Display student info (already masked by server)
                    lblStudentInfo.Text = $"Mã SV: {response.Student.StudentCode}  |  Họ tên: {response.Student.StudentName}  |  Lớp: {response.Student.StudentClass}";
                    lblStudentInfo.ForeColor = ThemeSuccess;

                    // Display scores with pass/fail indicators
                    var displayScores = response.Scores.Select(s => new
                    {
                        Học_kỳ = s.Semester,
                        Môn_học = s.SubjectName,
                        Tín_chỉ = s.SubjectCredits,
                        Điểm_CC = s.ScoreFirst,
                        Điểm_GK = s.ScoreSecond,
                        Điểm_CK = s.ScoreFinal,
                        Điểm_TK = s.ScoreOverall,
                        Điểm_chữ = s.ScoreText,
                        Trạng_thái = s.IsPassed ? "✓" : "✗",
                        IsPassed = s.IsPassed,
                        IsCurrentSemester = s.Semester == response.LastSemester
                    }).ToList();

                    dgvScores.DataSource = displayScores;
                    
                    // Hide helper columns
                    if (dgvScores.Columns.Contains("IsPassed"))
                    {
                        dgvScores.Columns["IsPassed"].Visible = false;
                    }
                    if (dgvScores.Columns.Contains("IsCurrentSemester"))
                    {
                        dgvScores.Columns["IsCurrentSemester"].Visible = false;
                    }
                    
                    // Highlight rows: failed = red, current semester = yellow
                    foreach (DataGridViewRow row in dgvScores.Rows)
                    {
                        bool isPassed = row.Cells["IsPassed"].Value != null && (bool)row.Cells["IsPassed"].Value;
                        bool isCurrentSem = row.Cells["IsCurrentSemester"].Value != null && (bool)row.Cells["IsCurrentSemester"].Value;
                        
                        if (!isPassed)
                        {
                            // Failed subjects - red highlight
                            row.DefaultCellStyle.BackColor = Color.FromArgb(120, 40, 40);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 200, 200);
                        }
                        else if (isCurrentSem)
                        {
                            // Current semester - yellow highlight
                            row.DefaultCellStyle.BackColor = Color.FromArgb(60, 80, 100);
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 220, 100);
                        }
                    }

                    // Display GPA, CPA
                    lblGPA.Text = $"GPA kỳ này ({response.LastSemester}): {response.GPA:F2}";
                    lblCPA.Text = $"CPA: {response.CPA:F2}";
                    lblTotalCredits.Text = $"Tổng tín chỉ hoàn thành: {response.TotalCredits}";
                }
                else
                {
                    string errorDetail = $"Không tìm thấy sinh viên!\n\n" +
                                       $"Mã sinh viên: {studentCode}\n" +
                                       $"Message: {response.Message}\n\n" +
                                       $"Vui lòng kiểm tra lại mã sinh viên.";
                    MessageBox.Show(errorDetail, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lblStudentInfo.Text = response.Message;
                    lblStudentInfo.ForeColor = Color.FromArgb(239, 83, 80);
                    dgvScores.DataSource = null;
                    lblGPA.Text = "GPA kỳ này: --";
                    lblCPA.Text = "CPA: --";
                    lblTotalCredits.Text = "Tổng tín chỉ hoàn thành: --";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tra cứu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSearch.Enabled = true;
                btnSearch.Text = "🔍  Tra cứu";
            }
        }

        private async void BtnStartServer_Click(object sender, EventArgs e)
        {
            try
            {
                btnStartServer.Enabled = false;
                lblServerStatus.Text = "● STARTING...";
                lblServerStatus.ForeColor = ThemeWarn;

                await Task.Delay(300);

                // Start server
                _ = Task.Run(() => serverService.Start(8888));
                isServerRunning = true;

                await Task.Delay(500);

                btnStopServer.Enabled = true;
                lblServerStatus.Text = "● RUNNING";
                lblServerStatus.ForeColor = ThemeSuccess;

                // Open server log window
                OpenServerLogWindow();
            }
            catch (Exception ex)
            {
                btnStartServer.Enabled = true;
                lblServerStatus.Text = "● ERROR";
                lblServerStatus.ForeColor = Color.FromArgb(239, 83, 80);
                MessageBox.Show("Lỗi khởi động server: " + ex.Message, "Lỗi");
            }
        }

        private void BtnStopServer_Click(object sender, EventArgs e)
        {
            try
            {
                if (serverLogForm != null && !serverLogForm.IsDisposed)
                {
                    serverLogForm.Close();
                }
                else
                {
                    serverService.Stop();
                    isServerRunning = false;

                    btnStartServer.Enabled = true;
                    btnStopServer.Enabled = false;
                    lblServerStatus.Text = "● STOPPED";
                    lblServerStatus.ForeColor = Color.FromArgb(239, 83, 80);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi dừng server: " + ex.Message, "Lỗi");
            }
        }

        private void OpenServerLogWindow()
        {
            serverLogForm = new Form
            {
                Text = "🖥️ SERVER LOG - Port 8888",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(Screen.PrimaryScreen.Bounds.Width - 820, 100),
                MinimumSize = new Size(600, 400),
                BackColor = Color.FromArgb(30, 30, 30)
            };

            Label lblHeader = new Label
            {
                Text = "● SERVER RUNNING - Transmission Log",
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Color.LimeGreen,
                BackColor = Color.FromArgb(20, 20, 20),
                Font = new Font("Consolas", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            serverLogForm.Controls.Add(lblHeader);

            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            serverLogForm.Controls.Add(pnlButtons);

            Button btnClearServerLog = new Button
            {
                Text = "Xóa Log",
                Location = new Point(15, 8),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9)
            };
            btnClearServerLog.FlatAppearance.BorderSize = 0;
            btnClearServerLog.Click += (s, ev) => txtServerLog?.Clear();
            pnlButtons.Controls.Add(btnClearServerLog);

            Button btnStopFromLog = new Button
            {
                Text = "⏹ Stop Server",
                Location = new Point(115, 8),
                Size = new Size(130, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnStopFromLog.FlatAppearance.BorderSize = 0;
            btnStopFromLog.Click += (s, ev) => serverLogForm?.Close();
            pnlButtons.Controls.Add(btnStopFromLog);

            txtServerLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9),
                WordWrap = false,
                BorderStyle = BorderStyle.None
            };
            serverLogForm.Controls.Add(txtServerLog);
            txtServerLog.BringToFront();

            serverLogForm.FormClosed += (s, ev) =>
            {
                txtServerLog = null;

                if (isServerRunning)
                {
                    serverService.Stop();
                    isServerRunning = false;

                    if (!this.IsDisposed)
                    {
                        btnStartServer.Enabled = true;
                        btnStopServer.Enabled = false;
                        lblServerStatus.Text = "● STOPPED";
                        lblServerStatus.ForeColor = Color.FromArgb(239, 83, 80);
                    }
                }
                serverLogForm = null;
            };

            serverLogForm.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isServerRunning)
            {
                serverService.Stop();
            }
            
            TransmissionLogger.OnClientLog -= LogClient;
            TransmissionLogger.OnServerLog -= LogServer;
            
            base.OnFormClosing(e);
        }

        private void BtnTestDB_Click(object sender, EventArgs e)
        {
            try
            {
                if (dbManager.TestConnection())
                {
                    int totalStudents = dbManager.GetTotalStudents();
                    var sampleCodes = dbManager.GetAllStudentCodes(5);
                    
                    string message = $"✓ Kết nối database thành công!\n\n" +
                                   $"Database: kmalegend\n" +
                                   $"Tổng số sinh viên: {totalStudents}\n\n" +
                                   $"Mẫu mã sinh viên:\n" +
                                   string.Join("\n", sampleCodes);
                    
                    MessageBox.Show(message, "Test Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("✗ Không thể kết nối database!", "Test Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"✗ Lỗi kết nối database:\n\n{ex.Message}", "Test Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvScores_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvScores.Rows[e.RowIndex];
            
            var scoreData = new
            {
                Học_kỳ = row.Cells["Học_kỳ"].Value?.ToString(),
                Môn_học = row.Cells["Môn_học"].Value?.ToString(),
                Tín_chỉ = row.Cells["Tín_chỉ"].Value,
                Điểm_CC = row.Cells["Điểm_CC"].Value,
                Điểm_GK = row.Cells["Điểm_GK"].Value,
                Điểm_CK = row.Cells["Điểm_CK"].Value,
                Điểm_TK = row.Cells["Điểm_TK"].Value,
                Điểm_chữ = row.Cells["Điểm_chữ"].Value?.ToString(),
                Trạng_thái = row.Cells["Trạng_thái"].Value?.ToString()
            };

            string json = System.Text.Json.JsonSerializer.Serialize(scoreData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            ShowJsonPopup("Chi tiết môn học", json);
        }

        private void ShowJsonPopup(string title, string json)
        {
            Form jsonForm = new Form
            {
                Text = title,
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                MinimumSize = new Size(400, 300)
            };

            TextBox txtJson = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 22, 35),
                ForeColor = Color.FromArgb(0, 230, 230),
                Font = new Font("Cascadia Code", 10),
                Text = json,
                WordWrap = false,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10)
            };
            jsonForm.Controls.Add(txtJson);

            Button btnClose = CreateModernButton("Đóng", Color.FromArgb(70, 73, 95), ThemeTextSecondary, 100, 35);
            btnClose.Dock = DockStyle.Bottom;
            btnClose.Click += (s, ev) => jsonForm.Close();
            jsonForm.Controls.Add(btnClose);

            jsonForm.ShowDialog();
        }

        private void BtnConfigDB_Click(object sender, EventArgs e)
        {
            Form configForm = new Form
            {
                Text = "Cấu hình Database",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int y = 20;

            Label lblTitle = new Label
            {
                Text = "CẤU HÌNH KẾT NỐI DATABASE",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            configForm.Controls.Add(lblTitle);
            y += 40;

            // Host
            Label lblHost = new Label
            {
                Text = "Host:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            configForm.Controls.Add(lblHost);

            TextBox txtHost = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(300, 28),
                Text = "36.50.54.109",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            configForm.Controls.Add(txtHost);
            y += 40;

            // Port
            Label lblPort = new Label
            {
                Text = "Port:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            configForm.Controls.Add(lblPort);

            TextBox txtPort = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(300, 28),
                Text = "3306",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            configForm.Controls.Add(txtPort);
            y += 40;

            // Database
            Label lblDatabase = new Label
            {
                Text = "Database:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            configForm.Controls.Add(lblDatabase);

            TextBox txtDatabase = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(300, 28),
                Text = "kmalegend",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            configForm.Controls.Add(txtDatabase);
            y += 40;

            // Username
            Label lblUsername = new Label
            {
                Text = "Username:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            configForm.Controls.Add(lblUsername);

            TextBox txtUsername = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(300, 28),
                Text = "anonymous",
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            configForm.Controls.Add(txtUsername);
            y += 40;

            // Password
            Label lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            configForm.Controls.Add(lblPassword);

            TextBox txtPassword = new TextBox
            {
                Location = new Point(150, y - 3),
                Size = new Size(300, 28),
                Text = "Thuanld@255",
                PasswordChar = '●',
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            configForm.Controls.Add(txtPassword);
            y += 50;

            // Connect button
            Button btnConnect = CreateModernButton("Kết nối", ThemeAccent, Color.White, 150, 38);
            btnConnect.Location = new Point(150, y);
            btnConnect.Font = new Font("Segoe UI Semibold", 11);
            btnConnect.Click += (s, ev) =>
            {
                try
                {
                    btnConnect.Enabled = false;
                    btnConnect.Text = "Đang kết nối...";

                    // Create new database manager with provided credentials
                    var testDbManager = new StudentDatabaseManager(
                        txtHost.Text.Trim(),
                        txtDatabase.Text.Trim(),
                        txtUsername.Text.Trim(),
                        txtPassword.Text,
                        int.Parse(txtPort.Text.Trim())
                    );

                    if (testDbManager.TestConnection())
                    {
                        int totalStudents = testDbManager.GetTotalStudents();
                        var sampleCodes = testDbManager.GetAllStudentCodes(3);

                        string message = $"Kết nối thành công!\n\n" +
                                       $"Host: {txtHost.Text}\n" +
                                       $"Port: {txtPort.Text}\n" +
                                       $"Database: {txtDatabase.Text}\n" +
                                       $"Username: {txtUsername.Text}\n\n" +
                                       $"Tổng số sinh viên: {totalStudents}\n\n" +
                                       $"Mẫu mã sinh viên:\n" +
                                       string.Join("\n", sampleCodes);

                        MessageBox.Show(message, "Kết nối thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Update the main dbManager
                        dbManager = testDbManager;
                        serverService = new StudentServerService(serverKeyPair, dbManager);
                    }
                    else
                    {
                        MessageBox.Show("Không thể kết nối database!\n\nVui lòng kiểm tra lại thông tin.", "Kết nối thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi kết nối:\n\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnConnect.Enabled = true;
                    btnConnect.Text = "Kết nối";
                }
            };
            configForm.Controls.Add(btnConnect);

            Button btnCancel = CreateModernButton("Đóng", Color.FromArgb(70, 73, 95), ThemeTextSecondary, 100, 38);
            btnCancel.Location = new Point(310, y);
            btnCancel.Click += (s, ev) => configForm.Close();
            configForm.Controls.Add(btnCancel);

            configForm.ShowDialog();
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

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            if (dgvScores.DataSource == null)
            {
                MessageBox.Show("Vui lòng tra cứu điểm trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Count grades and collect subjects by grade
            var gradeStats = new Dictionary<string, List<object>>
            {
                {"A+", new List<object>()}, {"A", new List<object>()}, {"B+", new List<object>()}, {"B", new List<object>()},
                {"C+", new List<object>()}, {"C", new List<object>()}, {"D+", new List<object>()}, {"D", new List<object>()}, {"F", new List<object>()}
            };

            foreach (DataGridViewRow row in dgvScores.Rows)
            {
                if (row.Cells["Điểm_chữ"].Value != null)
                {
                    string grade = row.Cells["Điểm_chữ"].Value.ToString();
                    if (gradeStats.ContainsKey(grade))
                    {
                        var subject = new
                        {
                            Học_kỳ = row.Cells["Học_kỳ"].Value?.ToString(),
                            Môn_học = row.Cells["Môn_học"].Value?.ToString(),
                            Tín_chỉ = row.Cells["Tín_chỉ"].Value,
                            Điểm_CC = row.Cells["Điểm_CC"].Value,
                            Điểm_GK = row.Cells["Điểm_GK"].Value,
                            Điểm_CK = row.Cells["Điểm_CK"].Value,
                            Điểm_TK = row.Cells["Điểm_TK"].Value,
                            Điểm_chữ = grade
                        };
                        gradeStats[grade].Add(subject);
                    }
                }
            }

            Form statsForm = new Form
            {
                Text = "Thống kê điểm",
                Size = new Size(500, 550),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int y = 20;
            Label lblTitle = new Label
            {
                Text = "THỐNG KÊ PHÂN BỐ ĐIỂM",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            statsForm.Controls.Add(lblTitle);
            y += 40;

            foreach (var stat in gradeStats)
            {
                Panel pnlStat = new Panel
                {
                    Location = new Point(20, y),
                    Size = new Size(440, 40),
                    BackColor = ThemePanel,
                    Cursor = Cursors.Hand
                };

                Label lblGrade = new Label
                {
                    Text = $"{stat.Key}:",
                    Location = new Point(15, 10),
                    Size = new Size(60, 20),
                    Font = new Font("Segoe UI Semibold", 11),
                    ForeColor = ThemeTextPrimary
                };
                pnlStat.Controls.Add(lblGrade);

                Label lblCount = new Label
                {
                    Text = $"{stat.Value.Count} môn",
                    Location = new Point(80, 10),
                    Size = new Size(100, 20),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = ThemeSuccess,
                    Cursor = Cursors.Hand
                };
                pnlStat.Controls.Add(lblCount);

                // Click event to show JSON
                string currentGrade = stat.Key;
                var subjects = stat.Value;
                lblCount.Click += (s, ev) =>
                {
                    if (subjects.Count > 0)
                    {
                        string json = System.Text.Json.JsonSerializer.Serialize(subjects, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        ShowJsonPopup($"Danh sách môn học điểm {currentGrade}", json);
                    }
                    else
                    {
                        MessageBox.Show($"Không có môn học nào đạt điểm {currentGrade}", "Thông báo");
                    }
                };

                pnlStat.Click += (s, ev) =>
                {
                    if (subjects.Count > 0)
                    {
                        string json = System.Text.Json.JsonSerializer.Serialize(subjects, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        ShowJsonPopup($"Danh sách môn học điểm {currentGrade}", json);
                    }
                };

                // Progress bar
                int barWidth = (int)(250.0 * stat.Value.Count / Math.Max(1, dgvScores.Rows.Count));
                Panel pnlBar = new Panel
                {
                    Location = new Point(180, 12),
                    Size = new Size(barWidth, 16),
                    BackColor = ThemeAccent
                };
                pnlStat.Controls.Add(pnlBar);

                statsForm.Controls.Add(pnlStat);
                y += 45;
            }

            statsForm.ShowDialog();
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
                ForeColor = ThemeAccentHover
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
                Text = lblCPA.Text.Contains(":") ? lblCPA.Text.Split(':')[1].Trim() : "0.00",
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
                ForeColor = ThemeAccentHover
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
                    ForeColor = ThemeSuccess
                };
                cpaForm.Controls.Add(lblValue);

                slider.ValueChanged += (s, ev) => lblValue.Text = $"{slider.Value}%";

                gradeSliders[grade] = (slider, lblValue, gradeSliders[grade].gradePoint);
                y += 50;
            }

            // Calculate button
            Button btnCalculate = CreateModernButton("🎯 Tính toán", ThemeAccent, Color.White, 150, 38);
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

        private async void BtnViewTimetable_Click(object sender, EventArgs e)
        {
            // Check if already logged in with cached credentials
            if (Utils.SessionManager.HasCachedCredentials())
            {
                // Don't auto-load, just show login dialog to confirm
                ShowActvnLoginDialog();
                return;
            }

            // Show login dialog for ACTVN Portal
            ShowActvnLoginDialog();
        }

        private async Task LoadTimetableWithCachedCredentials()
        {
            try
            {
                // Show loading message
                Form loadingForm = new Form
                {
                    Text = "Đang tải...",
                    Size = new Size(300, 100),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = ThemeBg,
                    FormBorderStyle = FormBorderStyle.None
                };

                Label lblLoading = new Label
                {
                    Text = "Đang tải lịch học...",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11),
                    ForeColor = ThemeTextPrimary
                };
                loadingForm.Controls.Add(lblLoading);
                loadingForm.Show();

                var scheduleResponse = await clientService.SendActvnScheduleRequestAsync(
                    Utils.SessionManager.Username, 
                    Utils.SessionManager.Password
                );

                loadingForm.Close();

                if (scheduleResponse.Success)
                {
                    var timetableForm = new TimetableCalendarView(scheduleResponse);
                    timetableForm.FormClosed += (s, e) => 
                    {
                        // When timetable form closes, user might want to view it again
                        Console.WriteLine("[ScoreLookup] Timetable form closed, credentials still cached");
                    };
                    timetableForm.Show();
                }
                else
                {
                    MessageBox.Show($"Không thể tải lịch học!\n\n{scheduleResponse.Message}\n\nVui lòng đăng nhập lại.", 
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Utils.SessionManager.Logout();
                    
                    // Hide logout button
                    if (btnLogout != null)
                        btnLogout.Visible = false;
                    
                    ShowActvnLoginDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}\n\nVui lòng đăng nhập lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Utils.SessionManager.Logout();
                
                // Hide logout button
                if (btnLogout != null)
                    btnLogout.Visible = false;
            }
        }

        private void ShowActvnLoginDialog()
        {
            Form loginForm = new Form
            {
                Text = "Đăng nhập ACTVN Portal",
                Size = new Size(450, 250),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblTitle = new Label
            {
                Text = "ĐĂNG NHẬP ACTVN PORTAL",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            loginForm.Controls.Add(lblTitle);

            Label lblUsername = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new Point(20, 60),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            loginForm.Controls.Add(lblUsername);

            TextBox txtUsername = new TextBox
            {
                Location = new Point(150, 57),
                Size = new Size(250, 28),
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            loginForm.Controls.Add(txtUsername);

            Label lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(20, 100),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextSecondary
            };
            loginForm.Controls.Add(lblPassword);

            TextBox txtPassword = new TextBox
            {
                Location = new Point(150, 97),
                Size = new Size(250, 28),
                PasswordChar = '●',
                BackColor = ThemeInput,
                ForeColor = ThemeTextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            loginForm.Controls.Add(txtPassword);

            Button btnLogin = CreateModernButton("Đăng nhập", ThemeAccent, Color.White, 120, 38);
            btnLogin.Location = new Point(150, 145);
            btnLogin.Font = new Font("Segoe UI Semibold", 11);
            loginForm.Controls.Add(btnLogin);

            Button btnCancel = CreateModernButton("Hủy", Color.FromArgb(70, 73, 95), ThemeTextSecondary, 100, 38);
            btnCancel.Location = new Point(280, 145);
            btnCancel.Click += (s, ev) => loginForm.Close();
            loginForm.Controls.Add(btnCancel);

            btnLogin.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnLogin.Enabled = false;
                btnLogin.Text = "Đang đăng nhập...";

                try
                {
                    var scheduleResponse = await clientService.SendActvnScheduleRequestAsync(txtUsername.Text, txtPassword.Text);

                    if (scheduleResponse.Success)
                    {
                        // Cache credentials after successful login
                        Utils.SessionManager.Login(txtUsername.Text, txtPassword.Text);
                        
                        // Show logout button
                        if (btnLogout != null)
                            btnLogout.Visible = true;
                        
                        MessageBox.Show("Đăng nhập thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        loginForm.Close();

                        // Open timetable form
                        var timetableForm = new TimetableCalendarView(scheduleResponse);
                        timetableForm.Show();
                    }
                    else
                    {
                        MessageBox.Show($"Đăng nhập thất bại!\n\n{scheduleResponse.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnLogin.Enabled = true;
                        btnLogin.Text = "Đăng nhập";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Đăng nhập";
                }
            };

            loginForm.ShowDialog();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (!Utils.SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Bạn chưa đăng nhập!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn đăng xuất?\n\nTài khoản: {Utils.SessionManager.Username}",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                Utils.SessionManager.Logout();
                
                // Hide logout button
                if (btnLogout != null)
                    btnLogout.Visible = false;
                
                MessageBox.Show("Đã đăng xuất thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnViewTranscript_Click(object sender, EventArgs e)
        {
            // Check if logged in
            if (!Utils.SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập trước khi xem bảng điểm ảo!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowActvnLoginDialog();
                return;
            }

            // Open Virtual Transcript Form với server key pair chung
            VirtualTranscriptForm virtualForm = new VirtualTranscriptForm(serverKeyPair);
            virtualForm.Show();
        }
    }
}
