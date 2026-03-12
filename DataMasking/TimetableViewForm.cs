using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DataMasking.Models;

namespace DataMasking
{
    public class TimetableViewForm : Form
    {
        private MonthCalendar calendar;
        private DataGridView dgvEvents;
        private Label lblStudentInfo;
        private Label lblSelectedDate;
        private Label lblMonthSummary;
        private ActvnScheduleResponse scheduleData;

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

        public TimetableViewForm(ActvnScheduleResponse data)
        {
            this.scheduleData = data;
            
            // Deduplicate events immediately
            DeduplicateEvents();
            
            InitializeComponents();
            LoadCalendarData();
        }

        private void DeduplicateEvents()
        {
            var uniqueEvents = new Dictionary<string, ActvnCalendarEvent>();
            int originalCount = scheduleData.Events.Count;
            
            foreach (var evt in scheduleData.Events)
            {
                string key = $"{evt.Date}|{evt.CourseCode}|{evt.Title}|{evt.Location}|{evt.Teacher}|{string.Join(",", evt.Lessons)}|{evt.StartTime}|{evt.EndTime}";
                if (!uniqueEvents.ContainsKey(key))
                {
                    uniqueEvents[key] = evt;
                }
            }
            
            scheduleData.Events = uniqueEvents.Values.OrderBy(e => e.Date).ThenBy(e => e.Lessons.Min()).ToList();
            
            Console.WriteLine($"[UI] Deduplication: {originalCount} -> {scheduleData.Events.Count} events (removed {originalCount - scheduleData.Events.Count} duplicates)");
        }

        private void InitializeComponents()
        {
            this.Text = "📅 Lịch học - ACTVN Portal";
            this.Size = new Size(1200, 700);
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
                Text = "📅  LỊCH HỌC - ACTVN PORTAL",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            pnlHeader.Controls.Add(lblTitle);

            lblStudentInfo = new Label
            {
                Text = $"SV: {scheduleData.StudentInfo.StudentName} ({scheduleData.StudentInfo.StudentCode})",
                Location = new Point(20, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = ThemeTextSecondary
            };
            pnlHeader.Controls.Add(lblStudentInfo);

            // Debug button
            Button btnViewJson = new Button
            {
                Text = "🔍 Xem JSON",
                Location = new Point(900, 15),
                Size = new Size(120, 35),
                BackColor = ThemeAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnViewJson.FlatAppearance.BorderSize = 0;
            btnViewJson.Click += BtnViewJson_Click;
            pnlHeader.Controls.Add(btnViewJson);

            // Week view button
            Button btnWeekView = new Button
            {
                Text = "📅 Xem tuần",
                Location = new Point(1030, 15),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnWeekView.FlatAppearance.BorderSize = 0;
            btnWeekView.Click += BtnWeekView_Click;
            pnlHeader.Controls.Add(btnWeekView);

            // Calendar Panel
            Panel pnlCalendar = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(350, 550),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlCalendar);

            Label lblCalTitle = new Label
            {
                Text = "Chọn ngày:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ThemeTextPrimary
            };
            pnlCalendar.Controls.Add(lblCalTitle);

            calendar = new MonthCalendar
            {
                Location = new Point(10, 40),
                MaxSelectionCount = 1
            };
            calendar.DateChanged += Calendar_DateChanged;
            pnlCalendar.Controls.Add(calendar);

            // Month summary
            lblMonthSummary = new Label
            {
                Text = "Tổng quan tháng:",
                Location = new Point(10, 230),
                Size = new Size(330, 300),
                Font = new Font("Segoe UI", 9),
                ForeColor = ThemeTextSecondary,
                BackColor = Color.FromArgb(35, 37, 52),
                Padding = new System.Windows.Forms.Padding(10)
            };
            pnlCalendar.Controls.Add(lblMonthSummary);

            // Events Panel
            Panel pnlEvents = new Panel
            {
                Location = new Point(390, 80),
                Size = new Size(780, 550),
                BackColor = ThemePanel
            };
            this.Controls.Add(pnlEvents);

            lblSelectedDate = new Label
            {
                Text = "Lịch học ngày: " + DateTime.Now.ToString("dd/MM/yyyy"),
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = ThemeAccentHover
            };
            pnlEvents.Controls.Add(lblSelectedDate);

            dgvEvents = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(760, 495),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            StyleDataGridView(dgvEvents);
            pnlEvents.Controls.Add(dgvEvents);
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

        private void LoadCalendarData()
        {
            // Highlight dates with events (already deduplicated)
            var eventDates = scheduleData.Events
                .Select(e => DateTime.Parse(e.Date))
                .Distinct()
                .ToArray();

            if (eventDates.Length > 0)
            {
                calendar.BoldedDates = eventDates;
            }

            // Load today's events
            LoadEventsForDate(DateTime.Now);
        }

        private void Calendar_DateChanged(object sender, DateRangeEventArgs e)
        {
            LoadEventsForDate(e.Start);
        }

        private void LoadEventsForDate(DateTime date)
        {
            lblSelectedDate.Text = "Lịch học ngày: " + date.ToString("dd/MM/yyyy (dddd)", new System.Globalization.CultureInfo("vi-VN"));

            string dateStr = date.ToString("yyyy-MM-dd");
            
            // Get events for this date (already deduplicated in constructor)
            var dayEvents = scheduleData.Events
                .Where(e => e.Date == dateStr)
                .OrderBy(e => e.Lessons.Min()) // Sort by earliest lesson
                .Select(e => new
                {
                    Giờ = $"{e.StartTime} - {e.EndTime}",
                    Tiết = string.Join(", ", e.Lessons),
                    Mã_môn = e.CourseCode,
                    Tên_môn = e.Title,
                    Phòng = e.Location,
                    Giảng_viên = e.Teacher
                })
                .ToList();

            dgvEvents.DataSource = dayEvents;

            if (dayEvents.Count == 0)
            {
                lblSelectedDate.Text += " - Không có lịch học";
            }
            else
            {
                lblSelectedDate.Text += $" - {dayEvents.Count} môn học";
            }

            // Update month summary
            UpdateMonthSummary(date);
        }

        private void UpdateMonthSummary(DateTime selectedDate)
        {
            // Events already deduplicated in constructor
            var monthEvents = scheduleData.Events
                .Where(e => DateTime.Parse(e.Date).Month == selectedDate.Month && 
                           DateTime.Parse(e.Date).Year == selectedDate.Year)
                .GroupBy(e => e.Date)
                .OrderBy(g => g.Key)
                .ToList();

            string summary = $"THÁNG {selectedDate.Month}/{selectedDate.Year}\n\n";
            summary += $"Tổng số ngày học: {monthEvents.Count}\n";
            summary += $"Tổng số buổi học: {scheduleData.Events.Count(e => DateTime.Parse(e.Date).Month == selectedDate.Month)}\n\n";
            summary += "Chi tiết:\n";

            foreach (var dayGroup in monthEvents.Take(10))
            {
                DateTime eventDate = DateTime.Parse(dayGroup.Key);
                string dayName = eventDate.ToString("dddd", new System.Globalization.CultureInfo("vi-VN"));
                summary += $"\n{eventDate:dd/MM} ({dayName}):\n";
                
                foreach (var evt in dayGroup.OrderBy(e => e.Lessons.Min()))
                {
                    summary += $"  • {evt.StartTime}-{evt.EndTime}: {evt.Title}\n";
                }
            }

            if (monthEvents.Count > 10)
            {
                summary += $"\n... và {monthEvents.Count - 10} ngày khác";
            }

            lblMonthSummary.Text = summary;
        }

        private void BtnViewJson_Click(object sender, EventArgs e)
        {
            try
            {
                // Serialize to JSON with formatting
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(scheduleData, jsonOptions);
                
                // Create form to display JSON
                Form jsonForm = new Form
                {
                    Text = "📋 Raw JSON Data",
                    Size = new Size(900, 700),
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = ThemeBg,
                    ForeColor = ThemeTextPrimary
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
                btnCopy.FlatAppearance.BorderSize = 0;
                btnCopy.Click += (s, ev) =>
                {
                    Clipboard.SetText(json);
                    MessageBox.Show("Đã copy JSON vào clipboard!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                Button btnStats = new Button
                {
                    Text = "📊 Thống kê",
                    Location = new Point(120, 10),
                    Size = new Size(120, 30),
                    BackColor = ThemeAccent,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnStats.FlatAppearance.BorderSize = 0;
                btnStats.Click += (s, ev) =>
                {
                    var stats = AnalyzeScheduleData();
                    MessageBox.Show(stats, "Thống kê dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                pnlButtons.Controls.Add(btnCopy);
                pnlButtons.Controls.Add(btnStats);
                jsonForm.Controls.Add(txtJson);
                jsonForm.Controls.Add(pnlButtons);
                jsonForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hiển thị JSON: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string AnalyzeScheduleData()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== THỐNG KÊ DỮ LIỆU LỊCH HỌC ===\n");
            
            stats.AppendLine($"Tổng số events: {scheduleData.Events.Count} (đã loại bỏ duplicate)");
            
            // Group by date
            var byDate = scheduleData.Events.GroupBy(e => e.Date).OrderBy(g => g.Key);
            stats.AppendLine($"Số ngày có lịch: {byDate.Count()}\n");
            
            // Date range
            if (scheduleData.Events.Any())
            {
                var firstDate = DateTime.Parse(scheduleData.Events.Min(e => e.Date));
                var lastDate = DateTime.Parse(scheduleData.Events.Max(e => e.Date));
                stats.AppendLine($"Từ ngày: {firstDate:dd/MM/yyyy}");
                stats.AppendLine($"Đến ngày: {lastDate:dd/MM/yyyy}\n");
            }
            
            // Events by weekday
            stats.AppendLine("Phân bố theo thứ:");
            var byWeekday = scheduleData.Events
                .GroupBy(e => e.DayOfWeek)
                .OrderBy(g => g.Key);
            
            string[] weekdayNames = { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
            foreach (var group in byWeekday)
            {
                string dayName = group.Key >= 0 && group.Key < weekdayNames.Length 
                    ? weekdayNames[group.Key] 
                    : $"Day{group.Key}";
                stats.AppendLine($"  {dayName}: {group.Count()} buổi");
            }
            
            // Courses
            var courses = scheduleData.Events
                .GroupBy(e => new { e.CourseCode, e.Title })
                .OrderBy(g => g.Key.CourseCode)
                .ToList();
            
            stats.AppendLine($"\nSố môn học: {courses.Count}");
            stats.AppendLine("\nDanh sách môn học:");
            foreach (var course in courses)
            {
                int sessionCount = course.Count();
                stats.AppendLine($"  • {course.Key.CourseCode} - {course.Key.Title} ({sessionCount} buổi)");
            }
            
            return stats.ToString();
        }

        private void BtnWeekView_Click(object sender, EventArgs e)
        {
            try
            {
                var weekView = new TimetableWeekView(scheduleData);
                weekView.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở week view: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
