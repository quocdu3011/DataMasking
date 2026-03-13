using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.IO;
using DataMasking.Models;

namespace DataMasking
{
    public class TimetableCalendarView : Form
    {
        private ActvnScheduleResponse scheduleData;
        private DateTime currentDate;
        private Panel pnlCalendar;
        private Label lblMonthYear;
        private Dictionary<string, Color> courseColors;
        private bool isMonthView = true;

        // Theme colors
        private static readonly Color ThemeBg = Color.FromArgb(30, 30, 46);
        private static readonly Color ThemePanel = Color.FromArgb(40, 42, 58);
        private static readonly Color ThemeHeader = Color.FromArgb(24, 24, 37);
        private static readonly Color ThemeAccent = Color.FromArgb(0, 150, 136);
        private static readonly Color ThemeAccentHover = Color.FromArgb(0, 180, 160);
        private static readonly Color ThemeTextPrimary = Color.FromArgb(230, 230, 240);
        private static readonly Color ThemeTextSecondary = Color.FromArgb(160, 165, 185);

        public TimetableCalendarView(ActvnScheduleResponse data)
        {
            this.scheduleData = data;
            this.currentDate = DateTime.Now;
            this.courseColors = new Dictionary<string, Color>();
            
            DeduplicateEvents();
            InitializeComponents();
            LoadCalendarView();
        }

        private void DeduplicateEvents()
        {
            var uniqueEvents = new Dictionary<string, ActvnCalendarEvent>();
            foreach (var evt in scheduleData.Events)
            {
                string key = $"{evt.Date}|{evt.CourseCode}|{evt.Title}|{evt.Location}|{evt.Teacher}|{string.Join(",", evt.Lessons)}";
                if (!uniqueEvents.ContainsKey(key))
                    uniqueEvents[key] = evt;
            }
            scheduleData.Events = uniqueEvents.Values.OrderBy(e => e.Date).ThenBy(e => e.Lessons.Min()).ToList();
        }

        private void InitializeComponents()
        {
            this.Text = "📅 Lịch học - ACTVN Portal";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ThemeBg;
            this.ForeColor = ThemeTextPrimary;
            this.Font = new Font("Segoe UI", 9);
            this.WindowState = FormWindowState.Maximized;

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = ThemeHeader
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "📅  LỊCH HỌC - ACTVN PORTAL",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblStudentInfo = new Label
            {
                Text = $"SV: {scheduleData.StudentInfo.StudentName} ({scheduleData.StudentInfo.StudentCode}) | Sinh: {scheduleData.StudentInfo.Birthday}",
                Location = new Point(20, 50),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = ThemeTextSecondary
            };
            pnlHeader.Controls.Add(lblStudentInfo);

            // Navigation and control buttons
            int btnX = 1200;
            Button btnPrev = CreateButton("◀", new Point(btnX, 15), 50);
            btnPrev.Click += (s, e) => { NavigatePeriod(-1); };
            pnlHeader.Controls.Add(btnPrev);

            lblMonthYear = new Label
            {
                Location = new Point(btnX + 60, 15),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeTextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = ThemePanel
            };
            pnlHeader.Controls.Add(lblMonthYear);

            Button btnNext = CreateButton("▶", new Point(btnX + 270, 15), 50);
            btnNext.Click += (s, e) => { NavigatePeriod(1); };
            pnlHeader.Controls.Add(btnNext);

            Button btnToday = CreateButton("Hôm nay", new Point(btnX + 330, 15), 90);
            btnToday.Click += (s, e) => { currentDate = DateTime.Now; LoadCalendarView(); };
            pnlHeader.Controls.Add(btnToday);

            // View toggle buttons
            Button btnMonthView = CreateButton("📅 Tháng", new Point(btnX, 55), 100);
            btnMonthView.BackColor = Color.FromArgb(33, 150, 243);
            btnMonthView.Click += (s, e) => { isMonthView = true; LoadCalendarView(); };
            pnlHeader.Controls.Add(btnMonthView);

            Button btnWeekView = CreateButton("📆 Tuần", new Point(btnX + 110, 55), 100);
            btnWeekView.BackColor = Color.FromArgb(156, 39, 176);
            btnWeekView.Click += (s, e) => { isMonthView = false; LoadCalendarView(); };
            pnlHeader.Controls.Add(btnWeekView);

            Button btnExportICS = CreateButton("💾 Xuất ICS", new Point(btnX + 220, 55), 110);
            btnExportICS.BackColor = Color.FromArgb(255, 87, 34);
            btnExportICS.Click += BtnExportICS_Click;
            pnlHeader.Controls.Add(btnExportICS);

            Button btnViewJson = CreateButton("🔍 JSON", new Point(btnX + 340, 55), 80);
            btnViewJson.Click += BtnViewJson_Click;
            pnlHeader.Controls.Add(btnViewJson);

            // Calendar panel
            pnlCalendar = new Panel
            {
                Location = new Point(10, 100),
                Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 110),
                BackColor = ThemePanel,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(pnlCalendar);
        }

        private Button CreateButton(string text, Point location, int width = 120)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 30),
                BackColor = ThemeAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void NavigatePeriod(int direction)
        {
            if (isMonthView)
                currentDate = currentDate.AddMonths(direction);
            else
                currentDate = currentDate.AddDays(direction * 7);
            LoadCalendarView();
        }

        private void LoadCalendarView()
        {
            pnlCalendar.Controls.Clear();
            
            if (isMonthView)
                LoadMonthView();
            else
                LoadWeekView();
        }

        private void LoadMonthView()
        {
            lblMonthYear.Text = currentDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("vi-VN"));

            DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
            int startDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; // Monday = 0

            int cols = 7;
            int rows = (int)Math.Ceiling((daysInMonth + startDayOfWeek) / 7.0);
            
            // Force panel to update its size before calculating
            pnlCalendar.PerformLayout();
            
            int cellWidth = (pnlCalendar.ClientSize.Width - 20) / cols;
            int cellHeight = Math.Min(130, Math.Max(110, (pnlCalendar.ClientSize.Height - 60) / rows));

            // Day headers
            string[] dayNames = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" };
            for (int i = 0; i < 7; i++)
            {
                Label lblDay = new Label
                {
                    Text = dayNames[i],
                    Location = new Point(10 + i * cellWidth, 10),
                    Size = new Size(cellWidth, 30),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = ThemeAccentHover,
                    BackColor = ThemeHeader
                };
                pnlCalendar.Controls.Add(lblDay);
            }

            // Calendar cells
            int dayCounter = 1;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int cellIndex = row * cols + col;
                    
                    if (cellIndex >= startDayOfWeek && dayCounter <= daysInMonth)
                    {
                        DateTime cellDate = new DateTime(currentDate.Year, currentDate.Month, dayCounter);
                        Panel dayCell = CreateDayCell(cellDate, col, row, cellWidth, cellHeight);
                        pnlCalendar.Controls.Add(dayCell);
                        dayCounter++;
                    }
                }
            }
        }

        private Panel CreateDayCell(DateTime date, int col, int row, int width, int height)
        {
            bool isToday = date.Date == DateTime.Now.Date;
            string dateStr = date.ToString("yyyy-MM-dd");
            
            Panel cell = new Panel
            {
                Location = new Point(10 + col * width, 45 + row * height),
                Size = new Size(width - 2, height - 2),
                BackColor = isToday ? Color.FromArgb(50, 0, 150, 136) : Color.FromArgb(35, 37, 52),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblDate = new Label
            {
                Text = date.Day.ToString(),
                Location = new Point(5, 5),
                Size = new Size(40, 25),
                Font = new Font("Segoe UI", 12, isToday ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isToday ? ThemeAccentHover : ThemeTextPrimary,
                BackColor = Color.Transparent
            };
            cell.Controls.Add(lblDate);

            // Get events for this day
            var dayEvents = scheduleData.Events
                .Where(e => e.Date == dateStr)
                .OrderBy(e => e.Lessons.Min())
                .ToList();

            int eventY = 35;
            int eventHeight = 28;
            int eventSpacing = 3;
            int maxEvents = Math.Max(1, (height - 40) / (eventHeight + eventSpacing));
            
            for (int i = 0; i < Math.Min(dayEvents.Count, maxEvents); i++)
            {
                var evt = dayEvents[i];
                Panel eventPanel = CreateEventBadge(evt, width - 12, eventY, eventHeight);
                cell.Controls.Add(eventPanel);
                eventY += eventHeight + eventSpacing;
            }

            if (dayEvents.Count > maxEvents)
            {
                Label lblMore = new Label
                {
                    Text = $"+{dayEvents.Count - maxEvents}",
                    Location = new Point(5, eventY),
                    Size = new Size(width - 12, 20),
                    Font = new Font("Segoe UI", 7, FontStyle.Italic),
                    ForeColor = ThemeAccentHover,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lblMore.Click += (s, e) => ShowDayEvents(date, dayEvents);
                cell.Controls.Add(lblMore);
            }

            return cell;
        }

        private Panel CreateEventBadge(ActvnCalendarEvent evt, int width, int yPos, int height = 28)
        {
            Color eventColor = GetCourseColor(evt.CourseCode);
            
            Panel badge = new Panel
            {
                Location = new Point(5, yPos),
                Size = new Size(width, height),
                BackColor = eventColor,
                Cursor = Cursors.Hand,
                Tag = evt
            };

            Label lblEvent = new Label
            {
                Text = $"⏰ {evt.StartTime} {(evt.Title.Length > 16 ? evt.Title.Substring(0, 13) + "..." : evt.Title)}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 5, 2, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            badge.Controls.Add(lblEvent);

            badge.Click += (s, e) => ShowEventDetails(evt);
            lblEvent.Click += (s, e) => ShowEventDetails(evt);

            badge.MouseEnter += (s, e) => badge.BackColor = LightenColor(eventColor, 30);
            badge.MouseLeave += (s, e) => badge.BackColor = eventColor;

            return badge;
        }

        private void LoadWeekView()
        {
            DateTime weekStart = GetWeekStart(currentDate);
            DateTime weekEnd = weekStart.AddDays(6);
            lblMonthYear.Text = $"{weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}";

            int timeColumnWidth = 80;
            int dayColumnWidth = (pnlCalendar.Width - timeColumnWidth - 30) / 7;
            int rowHeight = 100; // Tăng từ 80 lên 100
            int headerHeight = 50;

            // Day headers
            string[] dayNames = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN" };
            for (int i = 0; i < 7; i++)
            {
                DateTime day = weekStart.AddDays(i);
                bool isToday = day.Date == DateTime.Now.Date;
                
                Panel dayHeader = new Panel
                {
                    Location = new Point(timeColumnWidth + i * dayColumnWidth, 10),
                    Size = new Size(dayColumnWidth, headerHeight),
                    BackColor = isToday ? ThemeAccent : ThemeHeader,
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label lblDay = new Label
                {
                    Text = $"{dayNames[i]}\n{day:dd/MM}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.White
                };
                dayHeader.Controls.Add(lblDay);
                pnlCalendar.Controls.Add(dayHeader);
            }

            // Time slots
            string[] timeSlots = { "07:00", "09:35", "12:30", "15:00" };
            for (int slot = 0; slot < timeSlots.Length; slot++)
            {
                int yPos = headerHeight + 15 + slot * rowHeight;

                Label lblTime = new Label
                {
                    Text = timeSlots[slot],
                    Location = new Point(10, yPos + 25),
                    Size = new Size(timeColumnWidth - 10, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = ThemeTextSecondary
                };
                pnlCalendar.Controls.Add(lblTime);

                for (int day = 0; day < 7; day++)
                {
                    DateTime currentDay = weekStart.AddDays(day);
                    string dateStr = currentDay.ToString("yyyy-MM-dd");

                    Panel dayCell = new Panel
                    {
                        Location = new Point(timeColumnWidth + day * dayColumnWidth, yPos),
                        Size = new Size(dayColumnWidth, rowHeight),
                        BackColor = Color.FromArgb(35, 37, 52),
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    pnlCalendar.Controls.Add(dayCell);

                    var dayEvents = scheduleData.Events
                        .Where(e => e.Date == dateStr && IsInTimeSlot(e.StartTime, slot))
                        .ToList();

                    int eventY = 5;
                    foreach (var evt in dayEvents)
                    {
                        Panel eventPanel = CreateWeekEventPanel(evt, dayColumnWidth - 10, eventY);
                        dayCell.Controls.Add(eventPanel);
                        eventY += eventPanel.Height + 5; // Tăng spacing từ 3 lên 5
                    }
                }
            }
        }

        private Panel CreateWeekEventPanel(ActvnCalendarEvent evt, int width, int yPos)
        {
            Color eventColor = GetCourseColor(evt.CourseCode);
            
            Panel panel = new Panel
            {
                Location = new Point(5, yPos),
                Size = new Size(width, 65), // Tăng từ 50 lên 65
                BackColor = eventColor,
                Cursor = Cursors.Hand,
                Tag = evt
            };

            Label lblCourse = new Label
            {
                Text = evt.Title.Length > 22 ? evt.Title.Substring(0, 19) + "..." : evt.Title,
                Location = new Point(5, 4),
                Size = new Size(width - 10, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold), // Tăng từ 7 lên 8
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblCourse);

            Label lblTime = new Label
            {
                Text = $"⏰ {evt.StartTime}-{evt.EndTime}",
                Location = new Point(5, 24),
                Size = new Size(width - 10, 18),
                Font = new Font("Segoe UI", 7), // Tăng từ 6 lên 7
                ForeColor = Color.FromArgb(240, 240, 240),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblTime);

            Label lblLocation = new Label
            {
                Text = $"📍 {(evt.Location.Length > 15 ? evt.Location.Substring(0, 12) + "..." : evt.Location)}",
                Location = new Point(5, 42),
                Size = new Size(width - 10, 18),
                Font = new Font("Segoe UI", 7), // Tăng từ 6 lên 7
                ForeColor = Color.FromArgb(240, 240, 240),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblLocation);

            panel.Click += (s, e) => ShowEventDetails(evt);
            lblCourse.Click += (s, e) => ShowEventDetails(evt);
            lblTime.Click += (s, e) => ShowEventDetails(evt);
            lblLocation.Click += (s, e) => ShowEventDetails(evt);

            panel.MouseEnter += (s, e) => panel.BackColor = LightenColor(eventColor, 30);
            panel.MouseLeave += (s, e) => panel.BackColor = eventColor;

            return panel;
        }

        private bool IsInTimeSlot(string startTime, int slot)
        {
            if (string.IsNullOrEmpty(startTime)) return false;
            var time = TimeSpan.Parse(startTime);
            return slot switch
            {
                0 => time >= TimeSpan.Parse("07:00") && time < TimeSpan.Parse("09:35"),
                1 => time >= TimeSpan.Parse("09:35") && time < TimeSpan.Parse("12:30"),
                2 => time >= TimeSpan.Parse("12:30") && time < TimeSpan.Parse("15:00"),
                3 => time >= TimeSpan.Parse("15:00") && time < TimeSpan.Parse("18:00"),
                _ => false
            };
        }

        private void ShowDayEvents(DateTime date, List<ActvnCalendarEvent> events)
        {
            Form dayForm = new Form
            {
                Text = $"📅 Lịch học ngày {date:dd/MM/yyyy}",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                ForeColor = ThemeTextPrimary
            };

            DataGridView dgv = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(560, 430),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ThemePanel,
                GridColor = Color.FromArgb(70, 75, 95),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ThemePanel,
                    ForeColor = ThemeTextPrimary,
                    SelectionBackColor = ThemeAccent,
                    SelectionForeColor = Color.White
                }
            };

            dgv.DataSource = events.Select(e => new
            {
                Giờ = $"{e.StartTime}-{e.EndTime}",
                Môn_học = e.Title,
                Phòng = e.Location,
                Giảng_viên = e.Teacher
            }).ToList();

            dayForm.Controls.Add(dgv);
            dayForm.ShowDialog(this);
        }

        private void ShowEventDetails(ActvnCalendarEvent evt)
        {
            Form detailForm = new Form
            {
                Text = "📋 Chi tiết lịch học",
                Size = new Size(550, 450),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeBg,
                ForeColor = ThemeTextPrimary,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            int yPos = 20;
            AddDetailLabel(detailForm, "Môn học:", evt.Title, ref yPos, true);
            AddDetailLabel(detailForm, "Mã môn:", evt.CourseCode, ref yPos);
            AddDetailLabel(detailForm, "Ngày:", DateTime.Parse(evt.Date).ToString("dd/MM/yyyy (dddd)", new System.Globalization.CultureInfo("vi-VN")), ref yPos);
            AddDetailLabel(detailForm, "Thời gian:", $"{evt.StartTime} - {evt.EndTime}", ref yPos);
            AddDetailLabel(detailForm, "Tiết học:", string.Join(", ", evt.Lessons), ref yPos);
            AddDetailLabel(detailForm, "Phòng học:", evt.Location, ref yPos);
            AddDetailLabel(detailForm, "Giảng viên:", evt.Teacher, ref yPos);

            Button btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(225, yPos + 20),
                Size = new Size(100, 35),
                BackColor = ThemeAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnClose.Click += (s, e) => detailForm.Close();
            detailForm.Controls.Add(btnClose);

            detailForm.ShowDialog(this);
        }

        private void AddDetailLabel(Form form, string label, string value, ref int yPos, bool isTitle = false)
        {
            Label lblLabel = new Label
            {
                Text = label,
                Location = new Point(20, yPos),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeTextSecondary
            };
            form.Controls.Add(lblLabel);

            Label lblValue = new Label
            {
                Text = value,
                Location = new Point(140, yPos),
                Size = new Size(380, isTitle ? 40 : 25),
                Font = new Font("Segoe UI", isTitle ? 11 : 9, isTitle ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isTitle ? ThemeAccentHover : ThemeTextPrimary
            };
            form.Controls.Add(lblValue);

            yPos += isTitle ? 45 : 30;
        }

        private void BtnExportICS_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "iCalendar files (*.ics)|*.ics",
                    FileName = $"LichHoc_{scheduleData.StudentInfo.StudentCode}_{DateTime.Now:yyyyMMdd}.ics",
                    Title = "Xuất lịch học ra file ICS"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string icsContent = GenerateICSContent();
                    File.WriteAllText(saveDialog.FileName, icsContent, Encoding.UTF8);
                    MessageBox.Show($"Đã xuất lịch học thành công!\n\nFile: {saveDialog.FileName}\n\nBạn có thể import file này vào Google Calendar, Outlook, hoặc các ứng dụng lịch khác.",
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất file ICS: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateICSContent()
        {
            StringBuilder ics = new StringBuilder();
            
            // ICS Header
            ics.AppendLine("BEGIN:VCALENDAR");
            ics.AppendLine("VERSION:2.0");
            ics.AppendLine("PRODID:-//ACTVN Portal//Lich Hoc//VN");
            ics.AppendLine("CALSCALE:GREGORIAN");
            ics.AppendLine("METHOD:PUBLISH");
            ics.AppendLine($"X-WR-CALNAME:Lịch học - {scheduleData.StudentInfo.StudentName}");
            ics.AppendLine("X-WR-TIMEZONE:Asia/Ho_Chi_Minh");

            // Add events
            foreach (var evt in scheduleData.Events)
            {
                DateTime eventDate = DateTime.Parse(evt.Date);
                string startTime = evt.StartTime.Replace(":", "");
                string endTime = evt.EndTime.Replace(":", "");
                
                string dtStart = $"{eventDate:yyyyMMdd}T{startTime}00";
                string dtEnd = $"{eventDate:yyyyMMdd}T{endTime}00";
                string dtStamp = DateTime.Now.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
                string uid = $"{evt.CourseCode}-{eventDate:yyyyMMdd}-{startTime}@actvn.edu.vn";

                ics.AppendLine("BEGIN:VEVENT");
                ics.AppendLine($"UID:{uid}");
                ics.AppendLine($"DTSTAMP:{dtStamp}");
                ics.AppendLine($"DTSTART:{dtStart}");
                ics.AppendLine($"DTEND:{dtEnd}");
                ics.AppendLine($"SUMMARY:{EscapeICSText(evt.Title)}");
                ics.AppendLine($"DESCRIPTION:Mã môn: {evt.CourseCode}\\nGiảng viên: {EscapeICSText(evt.Teacher)}\\nTiết: {string.Join(", ", evt.Lessons)}");
                ics.AppendLine($"LOCATION:{EscapeICSText(evt.Location)}");
                ics.AppendLine($"CATEGORIES:Lịch học");
                ics.AppendLine($"STATUS:CONFIRMED");
                ics.AppendLine("END:VEVENT");
            }

            ics.AppendLine("END:VCALENDAR");
            return ics.ToString();
        }

        private string EscapeICSText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\")
                       .Replace(",", "\\,")
                       .Replace(";", "\\;")
                       .Replace("\n", "\\n");
        }

        private void BtnViewJson_Click(object sender, EventArgs e)
        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(scheduleData, jsonOptions);
                
                Form jsonForm = new Form
                {
                    Text = "📋 Raw JSON Data",
                    Size = new Size(900, 700),
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = ThemeBg
                };

                TextBox txtJson = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9),
                    BackColor = Color.FromArgb(20, 20, 30),
                    ForeColor = Color.FromArgb(220, 220, 230),
                    Text = json,
                    ReadOnly = true,
                    WordWrap = false
                };

                Panel pnlButtons = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    BackColor = ThemeHeader
                };

                Button btnCopy = new Button
                {
                    Text = "📋 Copy",
                    Location = new Point(10, 10),
                    Size = new Size(100, 30),
                    BackColor = ThemeAccent,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnCopy.Click += (s, ev) =>
                {
                    Clipboard.SetText(json);
                    MessageBox.Show("Đã copy JSON!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                pnlButtons.Controls.Add(btnCopy);
                jsonForm.Controls.Add(txtJson);
                jsonForm.Controls.Add(pnlButtons);
                jsonForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Color GetCourseColor(string courseCode)
        {
            if (!courseColors.ContainsKey(courseCode))
            {
                int hue = (courseCode.GetHashCode() & 0x7FFFFFFF) % 360;
                courseColors[courseCode] = ColorFromHSV(hue, 0.65, 0.75);
            }
            return courseColors[courseCode];
        }

        private Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0) return Color.FromArgb(255, v, t, p);
            else if (hi == 1) return Color.FromArgb(255, q, v, p);
            else if (hi == 2) return Color.FromArgb(255, p, v, t);
            else if (hi == 3) return Color.FromArgb(255, p, q, v);
            else if (hi == 4) return Color.FromArgb(255, t, p, v);
            else return Color.FromArgb(255, v, p, q);
        }

        private Color LightenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(color.R + amount, 255),
                Math.Min(color.G + amount, 255),
                Math.Min(color.B + amount, 255)
            );
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
}
