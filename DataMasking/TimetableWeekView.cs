using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DataMasking.Models;

namespace DataMasking
{
    public class TimetableWeekView : Form
    {
        private ActvnScheduleResponse scheduleData;
        private DateTime currentWeekStart;
        private Panel pnlWeekView;
        private Label lblWeekRange;
        private Dictionary<string, Color> courseColors;
        private Random colorRandom;

        // Theme colors
        private static readonly Color ThemeBg = Color.FromArgb(30, 30, 46);
        private static readonly Color ThemePanel = Color.FromArgb(40, 42, 58);
        private static readonly Color ThemeHeader = Color.FromArgb(24, 24, 37);
        private static readonly Color ThemeAccent = Color.FromArgb(0, 150, 136);
        private static readonly Color ThemeAccentHover = Color.FromArgb(0, 180, 160);
        private static readonly Color ThemeTextPrimary = Color.FromArgb(230, 230, 240);
        private static readonly Color ThemeTextSecondary = Color.FromArgb(160, 165, 185);

        public TimetableWeekView(ActvnScheduleResponse data)
        {
            this.scheduleData = data;
            this.currentWeekStart = GetWeekStart(DateTime.Now);
            this.courseColors = new Dictionary<string, Color>();
            this.colorRandom = new Random();
            
            DeduplicateEvents();
            InitializeComponents();
            LoadWeekView();
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
            Console.WriteLine($"[WeekView] Deduplication: {originalCount} -> {scheduleData.Events.Count} events");
        }

        private void InitializeComponents()
        {
            this.Text = "📅 Lịch học tuần - ACTVN Portal";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ThemeBg;
            this.ForeColor = ThemeTextPrimary;
            this.Font = new Font("Segoe UI", 9);

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = ThemeHeader
            };
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label
            {
                Text = "📅  LỊCH HỌC TUẦN",
                Location = new Point(20, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = ThemeAccentHover
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblStudentInfo = new Label
            {
                Text = $"SV: {scheduleData.StudentInfo.StudentName} ({scheduleData.StudentInfo.StudentCode}) - Sinh: {scheduleData.StudentInfo.Birthday}",
                Location = new Point(20, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = ThemeTextSecondary
            };
            pnlHeader.Controls.Add(lblStudentInfo);

            // Navigation buttons
            Button btnPrevWeek = CreateButton("◀ Tuần trước", new Point(1050, 15));
            btnPrevWeek.Click += (s, e) => { currentWeekStart = currentWeekStart.AddDays(-7); LoadWeekView(); };
            pnlHeader.Controls.Add(btnPrevWeek);

            Button btnToday = CreateButton("Hôm nay", new Point(1180, 15));
            btnToday.Click += (s, e) => { currentWeekStart = GetWeekStart(DateTime.Now); LoadWeekView(); };
            pnlHeader.Controls.Add(btnToday);

            Button btnNextWeek = CreateButton("Tuần sau ▶", new Point(1260, 15));
            btnNextWeek.Click += (s, e) => { currentWeekStart = currentWeekStart.AddDays(7); LoadWeekView(); };
            pnlHeader.Controls.Add(btnNextWeek);

            lblWeekRange = new Label
            {
                Location = new Point(1050, 55),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeTextPrimary,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlHeader.Controls.Add(lblWeekRange);

            // Week view panel
            pnlWeekView = new Panel
            {
                Location = new Point(10, 90),
                Size = new Size(1370, 690),
                BackColor = ThemePanel,
                AutoScroll = true
            };
            this.Controls.Add(pnlWeekView);
        }

        private Button CreateButton(string text, Point location)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(110, 30),
                BackColor = ThemeAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void LoadWeekView()
        {
            pnlWeekView.Controls.Clear();
            
            DateTime weekEnd = currentWeekStart.AddDays(6);
            lblWeekRange.Text = $"{currentWeekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}";

            // Draw time labels (left column)
            int timeColumnWidth = 80;
            int dayColumnWidth = (pnlWeekView.Width - timeColumnWidth - 20) / 7;
            int rowHeight = 60;
            int headerHeight = 50;

            // Draw day headers
            string[] dayNames = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN" };
            for (int i = 0; i < 7; i++)
            {
                DateTime day = currentWeekStart.AddDays(i);
                Panel dayHeader = new Panel
                {
                    Location = new Point(timeColumnWidth + i * dayColumnWidth, 0),
                    Size = new Size(dayColumnWidth, headerHeight),
                    BackColor = ThemeHeader,
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label lblDay = new Label
                {
                    Text = $"{dayNames[i]}\n{day:dd/MM}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = day.Date == DateTime.Now.Date ? ThemeAccentHover : ThemeTextPrimary
                };
                dayHeader.Controls.Add(lblDay);
                pnlWeekView.Controls.Add(dayHeader);
            }

            // Draw time slots and events
            string[] timeSlots = { "07:00", "09:35", "12:30", "15:00" };
            for (int slot = 0; slot < timeSlots.Length; slot++)
            {
                int yPos = headerHeight + slot * rowHeight;

                // Time label
                Label lblTime = new Label
                {
                    Text = timeSlots[slot],
                    Location = new Point(10, yPos + 20),
                    Size = new Size(timeColumnWidth - 10, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 8),
                    ForeColor = ThemeTextSecondary
                };
                pnlWeekView.Controls.Add(lblTime);

                // Day columns
                for (int day = 0; day < 7; day++)
                {
                    DateTime currentDay = currentWeekStart.AddDays(day);
                    string dateStr = currentDay.ToString("yyyy-MM-dd");

                    Panel dayCell = new Panel
                    {
                        Location = new Point(timeColumnWidth + day * dayColumnWidth, yPos),
                        Size = new Size(dayColumnWidth, rowHeight),
                        BackColor = Color.FromArgb(35, 37, 52),
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    pnlWeekView.Controls.Add(dayCell);

                    // Find events for this day and time slot
                    var dayEvents = scheduleData.Events
                        .Where(e => e.Date == dateStr && IsInTimeSlot(e.StartTime, slot))
                        .ToList();

                    int eventY = 5;
                    foreach (var evt in dayEvents)
                    {
                        Panel eventPanel = CreateEventPanel(evt, dayColumnWidth - 10, eventY);
                        dayCell.Controls.Add(eventPanel);
                        eventY += eventPanel.Height + 3;
                    }
                }
            }
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

        private Panel CreateEventPanel(ActvnCalendarEvent evt, int width, int yPos)
        {
            Color eventColor = GetCourseColor(evt.CourseCode);
            
            Panel panel = new Panel
            {
                Location = new Point(5, yPos),
                Size = new Size(width, 45),
                BackColor = eventColor,
                Cursor = Cursors.Hand,
                Tag = evt
            };

            Label lblCourse = new Label
            {
                Text = evt.Title.Length > 25 ? evt.Title.Substring(0, 22) + "..." : evt.Title,
                Location = new Point(3, 2),
                Size = new Size(width - 6, 18),
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblCourse);

            Label lblTime = new Label
            {
                Text = $"⏰ {evt.StartTime}-{evt.EndTime}",
                Location = new Point(3, 20),
                Size = new Size(width - 6, 12),
                Font = new Font("Segoe UI", 6),
                ForeColor = Color.FromArgb(230, 230, 230),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblTime);

            Label lblLocation = new Label
            {
                Text = $"📍 {evt.Location}",
                Location = new Point(3, 32),
                Size = new Size(width - 6, 12),
                Font = new Font("Segoe UI", 6),
                ForeColor = Color.FromArgb(230, 230, 230),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblLocation);

            panel.Click += (s, e) => ShowEventDetails(evt);
            lblCourse.Click += (s, e) => ShowEventDetails(evt);
            lblTime.Click += (s, e) => ShowEventDetails(evt);
            lblLocation.Click += (s, e) => ShowEventDetails(evt);

            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(
                Math.Min(eventColor.R + 20, 255),
                Math.Min(eventColor.G + 20, 255),
                Math.Min(eventColor.B + 20, 255)
            );
            panel.MouseLeave += (s, e) => panel.BackColor = eventColor;

            return panel;
        }

        private void ShowEventDetails(ActvnCalendarEvent evt)
        {
            Form detailForm = new Form
            {
                Text = "📋 Chi tiết lịch học",
                Size = new Size(500, 400),
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
                Location = new Point(200, yPos + 20),
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
                Size = new Size(330, isTitle ? 40 : 25),
                Font = new Font("Segoe UI", isTitle ? 10 : 9, isTitle ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isTitle ? ThemeAccentHover : ThemeTextPrimary
            };
            form.Controls.Add(lblValue);

            yPos += isTitle ? 45 : 30;
        }

        private Color GetCourseColor(string courseCode)
        {
            if (!courseColors.ContainsKey(courseCode))
            {
                // Generate a pleasant color
                int hue = (courseCode.GetHashCode() & 0x7FFFFFFF) % 360;
                courseColors[courseCode] = ColorFromHSV(hue, 0.6, 0.7);
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

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }
}
