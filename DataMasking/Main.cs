using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DataMasking.Database;
using DataMasking.Masking;
using DataMasking.Key;
using DataMasking.Network;

namespace DataMasking
{
    public partial class Main : Form
    {
        private DatabaseManager dbManager;
        private MaskingService maskingService;
        private RSAKeyPair serverKeyPair;  // Server's RSA key pair
        private ClientService clientService;
        private ServerService serverService;
        
        // Controls
        private TabControl tabControl;
        private DataGridView dgvOriginal, dgvEncrypted;
        private TextBox txtName, txtEmail, txtPhone, txtCard, txtSSN, txtAddress;
        private TextBox txtTransmissionLog;
        private ComboBox cboEncryptionType;
        private Button btnAdd, btnMask, btnEncrypt, btnRefresh, btnDelete, btnInitDB, btnAddSample;
        private Label lblStatus;
        private TextBox txtConnectionString;

        public Main()
        {
            InitializeComponent();
            
            maskingService = new MaskingService();
            
            // Khởi tạo database manager với connection string mặc định
            dbManager = new DatabaseManager("36.50.54.109", "datamasking_db", "anonymous", "1", 3306);
            
            // Tạo RSA key pair cho server TRƯỚC KHI khởi tạo UI
            serverKeyPair = new RSAKeyPair(1024);
            
            // Khởi tạo Client và Server services
            clientService = new ClientService(serverKeyPair);  // Client chỉ có public key
            serverService = new ServerService(serverKeyPair, dbManager);  // Server có cả private key
            
            // Khởi tạo UI sau khi có serverKeyPair
            InitializeCustomComponents();
            
            // Subscribe to transmission logger
            TransmissionLogger.OnLog += LogTransmission;
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

        private void InitializeCustomComponents()
        {
            this.Text = "Data Masking System - Mô phỏng Client-Server với Mã hóa Kênh truyền";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Connection String
            Label lblConn = new Label { Text = "Connection String:", Location = new Point(20, 15), AutoSize = true };
            this.Controls.Add(lblConn);

            txtConnectionString = new TextBox
            {
                Location = new Point(140, 12),
                Size = new Size(600, 25),
                Text = "Server=36.50.54.109;Port=3306;Database=datamasking_db;Uid=anonymous;Pwd=1;"
            };
            this.Controls.Add(txtConnectionString);

            btnInitDB = new Button { Text = "Khởi tạo DB", Location = new Point(750, 10), Size = new Size(100, 30) };
            btnInitDB.Click += BtnInitDB_Click;
            this.Controls.Add(btnInitDB);

            btnAddSample = new Button { Text = "Thêm dữ liệu mẫu", Location = new Point(860, 10), Size = new Size(130, 30) };
            btnAddSample.Click += BtnAddSample_Click;
            this.Controls.Add(btnAddSample);

            // Tab Control
            tabControl = new TabControl
            {
                Location = new Point(20, 50),
                Size = new Size(900, 580)
            };

            // Tab 1: Nhập dữ liệu (Client)
            TabPage tabInput = new TabPage("Client - Nhập & Gửi Dữ liệu");
            InitializeInputTab(tabInput);
            tabControl.TabPages.Add(tabInput);

            // Tab 2: Xem dữ liệu gốc
            TabPage tabOriginal = new TabPage("Server - Dữ liệu Gốc");
            InitializeOriginalTab(tabOriginal);
            tabControl.TabPages.Add(tabOriginal);

            // Tab 3: Dữ liệu đã che giấu
            TabPage tabMasked = new TabPage("Server - Dữ liệu Masked");
            InitializeMaskedTab(tabMasked);
            tabControl.TabPages.Add(tabMasked);

            // Tab 4: Dữ liệu mã hóa
            TabPage tabEncrypted = new TabPage("Dữ liệu Mã hóa");
            InitializeEncryptedTab(tabEncrypted);
            tabControl.TabPages.Add(tabEncrypted);

            // Tab 5: Thông tin Keys
            TabPage tabKeys = new TabPage("Thông tin Keys");
            InitializeKeysTab(tabKeys);
            tabControl.TabPages.Add(tabKeys);

            this.Controls.Add(tabControl);

            // Transmission Log (bên phải)
            Label lblLog = new Label 
            { 
                Text = "KÊNH TRUYỀN (Transmission Log)", 
                Location = new Point(930, 50), 
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(lblLog);

            txtTransmissionLog = new TextBox
            {
                Location = new Point(930, 75),
                Size = new Size(440, 555),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(txtTransmissionLog);

            Button btnClearLog = new Button 
            { 
                Text = "Xóa Log", 
                Location = new Point(1270, 50), 
                Size = new Size(100, 20),
                Font = new Font("Arial", 8)
            };
            btnClearLog.Click += (s, e) => txtTransmissionLog.Clear();
            this.Controls.Add(btnClearLog);

            // Status
            lblStatus = new Label
            {
                Text = "Sẵn sàng - Server RSA Key đã được tạo",
                Location = new Point(20, 640),
                AutoSize = true,
                ForeColor = Color.Green,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(lblStatus);

            // Server Info
            string publicKeyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{serverKeyPair.N}|{serverKeyPair.E}"));
            string displayKey = publicKeyBase64.Length > 60 ? publicKeyBase64.Substring(0, 60) + "..." : publicKeyBase64;
            
            Label lblServerInfo = new Label
            {
                Text = $"Server Public Key (Base64): {displayKey}",
                Location = new Point(20, 665),
                Size = new Size(1350, 20),
                ForeColor = Color.Blue,
                Font = new Font("Consolas", 8)
            };
            this.Controls.Add(lblServerInfo);

            Label lblServerInfo2 = new Label
            {
                Text = $"RSA Key Size: 1024-bit | E (Public Exponent): {serverKeyPair.E}",
                Location = new Point(20, 685),
                AutoSize = true,
                ForeColor = Color.DarkGreen,
                Font = new Font("Arial", 8)
            };
            this.Controls.Add(lblServerInfo2);
        }

        private void InitializeInputTab(TabPage tab)
        {
            Label lblInfo = new Label
            {
                Text = "CLIENT: Nhập dữ liệu và gửi đến Server qua kênh truyền đã mã hóa",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            tab.Controls.Add(lblInfo);

            int y = 40;
            int labelX = 20, textX = 150, width = 400;

            Label lbl1 = new Label { Text = "Họ và tên:", Location = new Point(labelX, y), AutoSize = true };
            txtName = new TextBox { Location = new Point(textX, y), Size = new Size(width, 25), Text = "Huỳnh Ngọc Hải" };
            tab.Controls.Add(lbl1);
            tab.Controls.Add(txtName);

            y += 40;
            Label lbl2 = new Label { Text = "Email:", Location = new Point(labelX, y), AutoSize = true };
            txtEmail = new TextBox { Location = new Point(textX, y), Size = new Size(width, 25), Text = "mr.haihuynhngoc@gmail.com" };
            tab.Controls.Add(lbl2);
            tab.Controls.Add(txtEmail);

            y += 40;
            Label lbl3 = new Label { Text = "Số điện thoại:", Location = new Point(labelX, y), AutoSize = true };
            txtPhone = new TextBox { Location = new Point(textX, y), Size = new Size(width, 25), Text = "0969074102" };
            tab.Controls.Add(lbl3);
            tab.Controls.Add(txtPhone);

            y += 40;
            Label lbl4 = new Label { Text = "Thẻ tín dụng:", Location = new Point(labelX, y), AutoSize = true };
            txtCard = new TextBox { Location = new Point(textX, y), Size = new Size(width, 25), Text = "4532-7797-9668-8888" };
            tab.Controls.Add(lbl4);
            tab.Controls.Add(txtCard);

            y += 40;
            Label lbl5 = new Label { Text = "SSN:", Location = new Point(labelX, y), AutoSize = true };
            txtSSN = new TextBox { Location = new Point(textX, y), Size = new Size(width, 25), Text = "017204001788" };
            tab.Controls.Add(lbl5);
            tab.Controls.Add(txtSSN);

            y += 40;
            Label lbl6 = new Label { Text = "Địa chỉ:", Location = new Point(labelX, y), AutoSize = true };
            txtAddress = new TextBox { Location = new Point(textX, y), Size = new Size(width, 80), Multiline = true, Text = "Học viện Kỹ Thuật Mật Mã" };
            tab.Controls.Add(lbl6);
            tab.Controls.Add(txtAddress);

            y += 100;
            btnAdd = new Button 
            { 
                Text = "Gửi đến Server (Mã hóa)", 
                Location = new Point(textX, y), 
                Size = new Size(200, 40),
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnAdd.Click += BtnSendToServer_Click;
            tab.Controls.Add(btnAdd);

            Button btnClear = new Button { Text = "Xóa form", Location = new Point(textX + 210, y), Size = new Size(100, 40) };
            btnClear.Click += (s, e) => ClearInputs();
            tab.Controls.Add(btnClear);

            // Response area
            y += 60;
            Label lblResponse = new Label 
            { 
                Text = "Response từ Server (Dữ liệu đã che giấu):", 
                Location = new Point(labelX, y), 
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            tab.Controls.Add(lblResponse);

            TextBox txtResponse = new TextBox
            {
                Name = "txtResponse",
                Location = new Point(labelX, y + 25),
                Size = new Size(850, 150),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.LightYellow
            };
            tab.Controls.Add(txtResponse);
        }

        private void InitializeOriginalTab(TabPage tab)
        {
            dgvOriginal = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(1120, 470),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tab.Controls.Add(dgvOriginal);

            btnRefresh = new Button { Text = "Làm mới", Location = new Point(10, 10), Size = new Size(100, 30) };
            btnRefresh.Click += (s, e) => LoadOriginalData();
            tab.Controls.Add(btnRefresh);

            btnDelete = new Button { Text = "Xóa dòng chọn", Location = new Point(120, 10), Size = new Size(120, 30) };
            btnDelete.Click += BtnDelete_Click;
            tab.Controls.Add(btnDelete);
        }

        private void InitializeMaskedTab(TabPage tab)
        {
            Label lblInfo = new Label
            {
                Text = "Dữ liệu được che giấu (masking) để bảo vệ thông tin nhạy cảm khi hiển thị",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Italic)
            };
            tab.Controls.Add(lblInfo);

            DataGridView dgvMasked = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(1120, 480),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tab.Controls.Add(dgvMasked);

            Button btnShowMasked = new Button { Text = "Hiển thị dữ liệu che giấu", Location = new Point(10, 10), Size = new Size(180, 25) };
            btnShowMasked.Click += (s, e) => ShowMaskedData(dgvMasked);
            tab.Controls.Add(btnShowMasked);
        }

        private void InitializeEncryptedTab(TabPage tab)
        {
            Label lblType = new Label { Text = "Phương thức mã hóa:", Location = new Point(10, 15), AutoSize = true };
            tab.Controls.Add(lblType);

            cboEncryptionType = new ComboBox
            {
                Location = new Point(150, 12),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboEncryptionType.Items.AddRange(new object[] { "AES", "RSA", "Hybrid" });
            cboEncryptionType.SelectedIndex = 0;
            tab.Controls.Add(cboEncryptionType);

            btnEncrypt = new Button { Text = "Mã hóa dữ liệu", Location = new Point(260, 10), Size = new Size(120, 30) };
            btnEncrypt.Click += BtnEncrypt_Click;
            tab.Controls.Add(btnEncrypt);

            Button btnRefreshEnc = new Button { Text = "Làm mới", Location = new Point(390, 10), Size = new Size(100, 30) };
            btnRefreshEnc.Click += (s, e) => LoadEncryptedData();
            tab.Controls.Add(btnRefreshEnc);

            dgvEncrypted = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(870, 470),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tab.Controls.Add(dgvEncrypted);
        }

        private void InitializeKeysTab(TabPage tab)
        {
            Label lblTitle = new Label
            {
                Text = "THÔNG TIN KHÓA MÃ HÓA (CRYPTOGRAPHIC KEYS)",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            tab.Controls.Add(lblTitle);

            // RSA Public Key
            Label lblRSAPublic = new Label
            {
                Text = "RSA PUBLIC KEY (Server):",
                Location = new Point(20, 60),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            tab.Controls.Add(lblRSAPublic);

            TextBox txtRSAPublic = new TextBox
            {
                Location = new Point(20, 85),
                Size = new Size(850, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                Text = serverKeyPair.GetPublicKeyPEM()
            };
            tab.Controls.Add(txtRSAPublic);

            // RSA Key Info
            Label lblRSAInfo = new Label
            {
                Text = $"Key Size: 1024-bit | Modulus (N): {serverKeyPair.N.ToString().Length} digits | Public Exponent (E): {serverKeyPair.E}",
                Location = new Point(20, 210),
                AutoSize = true,
                Font = new Font("Arial", 8),
                ForeColor = Color.DarkGreen
            };
            tab.Controls.Add(lblRSAInfo);

            // Base64 format
            Label lblBase64 = new Label
            {
                Text = "PUBLIC KEY (Base64 Format):",
                Location = new Point(20, 240),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            tab.Controls.Add(lblBase64);

            TextBox txtBase64 = new TextBox
            {
                Location = new Point(20, 265),
                Size = new Size(850, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8),
                Text = serverKeyPair.GetPublicKeyBase64()
            };
            tab.Controls.Add(txtBase64);

            // Giải thích
            Label lblExplain = new Label
            {
                Text = "📌 Giải thích:\n" +
                       "• Public Key được chia sẻ công khai để Client mã hóa dữ liệu\n" +
                       "• Private Key được giữ bí mật ở Server để giải mã\n" +
                       "• Format Base64 giúp dễ dàng truyền qua mạng và lưu trữ\n" +
                       "• RSA 1024-bit đủ mạnh cho mục đích học tập (Production nên dùng 2048-bit+)",
                Location = new Point(20, 360),
                Size = new Size(850, 100),
                Font = new Font("Arial", 9),
                ForeColor = Color.DarkSlateGray
            };
            tab.Controls.Add(lblExplain);

            // Copy button
            Button btnCopyPublic = new Button
            {
                Text = "Copy Public Key",
                Location = new Point(20, 470),
                Size = new Size(150, 30)
            };
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
                    txtCard.Text, txtSSN.Text, txtAddress.Text
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
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Thông báo");
                    return;
                }

                // Clear log trước
                txtTransmissionLog.Clear();

                // CLIENT: Tạo và mã hóa request
                TransmissionPacket packet = clientService.CreateSecureRequest(
                    txtName.Text, txtEmail.Text, txtPhone.Text,
                    txtCard.Text, txtSSN.Text, txtAddress.Text
                );

                // Mô phỏng truyền qua mạng (delay nhỏ)
                System.Threading.Thread.Sleep(100);

                // SERVER: Nhận và xử lý request
                ServerResponse response = serverService.ProcessSecureRequest(packet);

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

                    lblStatus.Text = $"✓ Đã gửi và nhận response thành công! ID: {response.RecordId}";
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
            }
            catch (Exception ex)
            {
                lblStatus.Text = "✗ Lỗi gửi dữ liệu!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
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
                DataTable dt = dbManager.GetAllSensitiveData();
                DataTable maskedDt = new DataTable();
                
                maskedDt.Columns.Add("ID");
                maskedDt.Columns.Add("Họ tên (Masked)");
                maskedDt.Columns.Add("Email (Masked)");
                maskedDt.Columns.Add("Điện thoại (Masked)");
                maskedDt.Columns.Add("Thẻ tín dụng (Masked)");
                maskedDt.Columns.Add("SSN (Masked)");
                maskedDt.Columns.Add("Địa chỉ (Masked)");

                foreach (DataRow row in dt.Rows)
                {
                    maskedDt.Rows.Add(
                        row["id"],
                        maskingService.MaskName(row["full_name"].ToString()),
                        maskingService.MaskEmail(row["email"].ToString()),
                        maskingService.MaskPhone(row["phone"].ToString()),
                        maskingService.MaskCreditCard(row["credit_card"].ToString()),
                        maskingService.MaskSSN(row["ssn"].ToString()),
                        maskingService.MaskAddress(row["address"].ToString())
                    );
                }

                dgv.DataSource = maskedDt;
                lblStatus.Text = "Đã hiển thị dữ liệu che giấu";
                lblStatus.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi");
            }
        }

        private void ClearInputs()
        {
            txtName.Clear();
            txtEmail.Clear();
            txtPhone.Clear();
            txtCard.Clear();
            txtSSN.Clear();
            txtAddress.Clear();
        }
    }
}


