using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DataMasking.Utils;

namespace DataMasking.Services
{
    public class ActvnCrawlerService
    {
        private readonly string baseUrl = "http://qldt.actvn.edu.vn";
        private readonly HttpClient httpClient;
        private readonly CookieContainer cookieContainer;

        public ActvnCrawlerService()
        {
            cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Login to ACTVN portal
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            try
            {
                // Step 1: GET login page to extract hidden fields
                string loginUrl = $"{baseUrl}/CMCSoft.IU.Web.Info/Login.aspx";
                var getResponse = await httpClient.GetAsync(loginUrl);
                string loginPageHtml = await getResponse.Content.ReadAsStringAsync();

                // Parse hidden fields using HtmlAgilityPack
                var hiddenFields = ExtractHiddenFields(loginPageHtml);

                // Step 2: POST login credentials
                var formData = new Dictionary<string, string>(hiddenFields)
                {
                    ["txtUserName"] = username.ToUpper(),
                    ["txtPassword"] = MD5Hash.ComputeHash(password),
                    ["btnSubmit"] = "Đăng nhập"
                };

                var content = new FormUrlEncodedContent(formData);
                var postResponse = await httpClient.PostAsync(loginUrl, content);
                string responseHtml = await postResponse.Content.ReadAsStringAsync();

                // Check for error using HtmlAgilityPack
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(responseHtml);
                var errorNode = doc.DocumentNode.SelectSingleNode("//span[@id='lblErrorInfo']");

                if (errorNode != null && !string.IsNullOrWhiteSpace(errorNode.InnerText))
                {
                    return new LoginResult { Success = false, Message = errorNode.InnerText.Trim() };
                }

                return new LoginResult { Success = true, Message = "Đăng nhập thành công" };
            }
            catch (Exception ex)
            {
                return new LoginResult { Success = false, Message = $"Lỗi đăng nhập: {ex.Message}" };
            }
        }

        /// <summary>
        /// Fetch student profile information
        /// </summary>
        public async Task<StudentInfo> FetchStudentProfileAsync()
        {
            try
            {
                string profileUrl = $"{baseUrl}/CMCSoft.IU.Web.Info/StudentProfileNew/HoSoSinhVien.aspx";
                var response = await httpClient.GetAsync(profileUrl);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                string birthday = GetTextById(doc, "txtNgaySinh") ?? "";
                
                // Mask birthday: show only year, hide day and month
                string maskedBirthday = MaskBirthday(birthday);

                return new StudentInfo
                {
                    StudentCode  = GetTextById(doc, "txtMaSV")    ?? "",
                    StudentName  = GetTextById(doc, "txtHoTen")   ?? "",
                    Gender       = GetTextById(doc, "txtGioiTinh") ?? "",
                    Birthday     = maskedBirthday,
                    BankAccount  = GetTextById(doc, "txtSoTaiKhoanNganHang") ?? "",
                    IdCard       = GetTextById(doc, "txtCMTND")   ?? "",
                    BirthPlace   = GetTextById(doc, "txtNoiSinh") ?? "",
                    PersonalPhone = GetTextById(doc, "txtDienThoaiCaNhan") ?? "",
                    Email        = GetTextById(doc, "txtEmail")   ?? "",
                    EmergencyContact = GetTextById(doc, "txtKhiCanBaoTinChoAi") ?? ""
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi lấy thông tin sinh viên: {ex.Message}");
            }
        }

        private string MaskBirthday(string birthday)
        {
            if (string.IsNullOrEmpty(birthday))
                return "";
            
            // Format: dd/MM/yyyy -> **/**/yyyy
            var parts = birthday.Split('/');
            if (parts.Length == 3)
            {
                return $"**/**/{parts[2]}";
            }
            
            return birthday;
        }

        /// <summary>
        /// Fetch student timetable and parse to calendar events
        /// </summary>
        public async Task<StudentScheduleResponse> FetchStudentScheduleAsync()
        {
            try
            {
                var studentInfo = await FetchStudentProfileAsync();
                
                string timetableUrl = $"{baseUrl}/CMCSoft.IU.Web.Info/Reports/Form/StudentTimeTable.aspx";
                var response = await httpClient.GetAsync(timetableUrl);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var events = new List<CalendarEvent>();

                // Parse table rows - correct column indices based on Java code
                var rows = doc.DocumentNode.SelectNodes("//tr[@class='cssListItem' or @class='cssListAlternativeItem']");
                
                Console.WriteLine($"[ACTVN] Found {rows?.Count ?? 0} rows in timetable");
                
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells != null && cells.Count >= 6)
                        {
                            // Java code: cells[1]=courseName, cells[2]=courseCode, cells[3]=schedule, cells[4]=location, cells[5]=teacher
                            string courseName = cells[1].InnerText.Trim();
                            string courseCode = cells[2].InnerText.Trim();
                            // Get HTML content and replace <br> with newlines
                            string scheduleHtml = cells[3].InnerHtml;
                            string scheduleText = scheduleHtml.Replace("<br>", "\n").Replace("<BR>", "\n");
                            // Remove other HTML tags
                            scheduleText = Regex.Replace(scheduleText, "<.*?>", string.Empty).Trim();
                            string location = cells[4].InnerText.Trim();
                            string teacher = cells[5].InnerText.Trim();

                            Console.WriteLine($"\n[ACTVN] ========================================");
                            Console.WriteLine($"[ACTVN] Processing row for: {courseName} ({courseCode})");
                            Console.WriteLine($"[ACTVN] Location: {location}, Teacher: {teacher}");
                            Console.WriteLine($"[ACTVN] Schedule HTML: {scheduleHtml.Substring(0, Math.Min(100, scheduleHtml.Length))}...");
                            Console.WriteLine($"[ACTVN] Schedule Text: {scheduleText}");

                            // Parse schedule text to calendar events
                            var courseEvents = ParseScheduleToCalendarEvents(courseName, courseCode, scheduleText, location, teacher);
                            Console.WriteLine($"[ACTVN] Generated {courseEvents.Count} events for this course");
                            
                            if (courseEvents.Count > 0)
                            {
                                Console.WriteLine($"[ACTVN] Sample event: {courseEvents[0].Date} - Thứ {courseEvents[0].DayOfWeek} - Tiết {string.Join(",", courseEvents[0].Lessons)}");
                            }
                            
                            events.AddRange(courseEvents);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[ACTVN] No rows found - checking HTML structure");
                    var allRows = doc.DocumentNode.SelectNodes("//tr");
                    Console.WriteLine($"[ACTVN] Total rows in page: {allRows?.Count ?? 0}");
                }

                Console.WriteLine($"[ACTVN] Total events generated: {events.Count}");
                
                // Check for duplicates
                var duplicateCheck = events
                    .GroupBy(e => new { e.Date, e.CourseCode, e.Title, Lessons = string.Join(",", e.Lessons) })
                    .Where(g => g.Count() > 1)
                    .ToList();
                
                if (duplicateCheck.Any())
                {
                    Console.WriteLine($"[ACTVN] ⚠️ WARNING: Found {duplicateCheck.Count} duplicate groups!");
                    foreach (var dup in duplicateCheck.Take(3))
                    {
                        Console.WriteLine($"[ACTVN]   Duplicate: {dup.Key.Date} - {dup.Key.Title} (x{dup.Count()})");
                    }
                }

                return new StudentScheduleResponse
                {
                    StudentInfo = studentInfo,
                    Events = events
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACTVN] Error in FetchStudentScheduleAsync: {ex.Message}");
                Console.WriteLine($"[ACTVN] Stack trace: {ex.StackTrace}");
                throw new Exception($"Lỗi lấy lịch học: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse schedule text to calendar events
        /// Example: "Từ 19/01/2026 đến 08/02/2026: Thứ 2 tiết 10,11,12 Thứ 4 tiết 10,11,12"
        /// </summary>
        private List<CalendarEvent> ParseScheduleToCalendarEvents(string courseName, string courseCode, 
            string scheduleText, string location, string teacher)
        {
            var events = new List<CalendarEvent>();

            try
            {
                Console.WriteLine($"[ACTVN] Parsing schedule for {courseName}:");
                Console.WriteLine($"[ACTVN] Raw schedule text: {scheduleText}");
                
                // Parse schedule - can have multiple date ranges
                // Format: "Từ 19/01/2026 đến 08/02/2026:\nThứ 2 tiết 10,11,12\nThứ 4 tiết 10,11,12"
                // Or multiple ranges: "Từ 19/01/2026 đến 08/02/2026: (1) Thứ 2 tiết 10,11,12\nTừ 02/03/2026 đến 12/04/2026: (2) Thứ 2 tiết 10,11,12"
                
                // Split by "Từ" to handle multiple date ranges
                var rangePattern = @"Từ\s+(\d{2}/\d{2}/\d{4})\s+đến\s+(\d{2}/\d{2}/\d{4})[:\s]*(.+?)(?=Từ\s+\d{2}/\d{2}/\d{4}|$)";
                var rangeMatches = Regex.Matches(scheduleText, rangePattern, RegexOptions.Singleline);
                
                Console.WriteLine($"[ACTVN] Found {rangeMatches.Count} date range(s)");
                
                if (rangeMatches.Count == 0)
                {
                    Console.WriteLine($"[ACTVN] No date ranges found, trying simple split...");
                    // Fallback to simple parsing
                    return ParseScheduleSimple(courseName, courseCode, scheduleText, location, teacher);
                }
                
                foreach (Match rangeMatch in rangeMatches)
                {
                    string startDateStr = rangeMatch.Groups[1].Value;
                    string endDateStr = rangeMatch.Groups[2].Value;
                    string scheduleBody = rangeMatch.Groups[3].Value;
                    
                    Console.WriteLine($"[ACTVN] Processing range: {startDateStr} to {endDateStr}");
                    Console.WriteLine($"[ACTVN] Schedule body: {scheduleBody}");
                    
                    // Parse weekday schedules in this range
                    var weekdayMatches = Regex.Matches(scheduleBody, @"Thứ\s+(\d+)\s+tiết\s+([\d,]+)");
                    var weekdaySchedules = new List<(int weekday, List<int> lessons)>();
                    
                    foreach (Match weekdayMatch in weekdayMatches)
                    {
                        int weekday = int.Parse(weekdayMatch.Groups[1].Value);
                        var lessons = weekdayMatch.Groups[2].Value.Split(',').Select(int.Parse).ToList();
                        weekdaySchedules.Add((weekday, lessons));
                        Console.WriteLine($"[ACTVN]   Found: Thứ {weekday}, lessons {string.Join(",", lessons)}");
                    }
                    
                    if (weekdaySchedules.Count == 0)
                    {
                        Console.WriteLine($"[ACTVN]   No weekday schedules found in this range");
                        continue;
                    }
                    
                    // Generate events for this date range
                    DateTime startDate = DateTime.ParseExact(startDateStr, "dd/MM/yyyy", null);
                    DateTime endDate = DateTime.ParseExact(endDateStr, "dd/MM/yyyy", null);
                    
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        int dayOfWeek = GetVietnameseDayOfWeek(date);
                        
                        foreach (var schedule in weekdaySchedules)
                        {
                            if (schedule.weekday == dayOfWeek)
                            {
                                var lessonTimes = GetLessonTimes(schedule.lessons);
                                events.Add(new CalendarEvent
                                {
                                    Title = courseName,
                                    CourseCode = courseCode,
                                    Teacher = teacher,
                                    Location = location,
                                    Date = date.ToString("yyyy-MM-dd"),
                                    Lessons = schedule.lessons,
                                    StartTime = lessonTimes.start,
                                    EndTime = lessonTimes.end,
                                    DayOfWeek = dayOfWeek
                                });
                            }
                        }
                    }
                }
                
                Console.WriteLine($"[ACTVN] Generated {events.Count} events for {courseName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACTVN] Lỗi parse schedule: {ex.Message}");
                Console.WriteLine($"[ACTVN] Schedule text: {scheduleText}");
                Console.WriteLine($"[ACTVN] Stack trace: {ex.StackTrace}");
            }

            return events;
        }

        private List<CalendarEvent> ParseScheduleSimple(string courseName, string courseCode, 
            string scheduleText, string location, string teacher)
        {
            var events = new List<CalendarEvent>();
            
            try
            {
                string[] lines = scheduleText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"[ACTVN] Simple parse: Split into {lines.Length} lines");
                
                string startDateStr = "";
                string endDateStr = "";
                var weekdaySchedules = new List<(int weekday, List<int> lessons)>();

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    Console.WriteLine($"[ACTVN] Processing line: '{trimmedLine}'");
                    
                    if (trimmedLine.Contains("Từ") && trimmedLine.Contains("đến"))
                    {
                        var dateMatch = Regex.Match(trimmedLine, @"Từ\s+(\d{2}/\d{2}/\d{4})\s+đến\s+(\d{2}/\d{2}/\d{4})");
                        if (dateMatch.Success)
                        {
                            startDateStr = dateMatch.Groups[1].Value;
                            endDateStr = dateMatch.Groups[2].Value;
                            Console.WriteLine($"[ACTVN] Found date range: {startDateStr} to {endDateStr}");
                        }
                    }
                    else
                    {
                        var weekdayMatch = Regex.Match(trimmedLine, @"Thứ\s+(\d+)\s+tiết\s+([\d,]+)");
                        if (weekdayMatch.Success)
                        {
                            int weekday = int.Parse(weekdayMatch.Groups[1].Value);
                            var lessons = weekdayMatch.Groups[2].Value.Split(',').Select(int.Parse).ToList();
                            weekdaySchedules.Add((weekday, lessons));
                            Console.WriteLine($"[ACTVN] Found schedule: Thứ {weekday}, lessons {string.Join(",", lessons)}");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(startDateStr) && !string.IsNullOrEmpty(endDateStr) && weekdaySchedules.Count > 0)
                {
                    DateTime startDate = DateTime.ParseExact(startDateStr, "dd/MM/yyyy", null);
                    DateTime endDate = DateTime.ParseExact(endDateStr, "dd/MM/yyyy", null);

                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        int dayOfWeek = GetVietnameseDayOfWeek(date);
                        
                        foreach (var schedule in weekdaySchedules)
                        {
                            if (schedule.weekday == dayOfWeek)
                            {
                                var lessonTimes = GetLessonTimes(schedule.lessons);
                                events.Add(new CalendarEvent
                                {
                                    Title = courseName,
                                    CourseCode = courseCode,
                                    Teacher = teacher,
                                    Location = location,
                                    Date = date.ToString("yyyy-MM-dd"),
                                    Lessons = schedule.lessons,
                                    StartTime = lessonTimes.start,
                                    EndTime = lessonTimes.end,
                                    DayOfWeek = dayOfWeek
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACTVN] Error in simple parse: {ex.Message}");
            }
            
            return events;
        }

        // Helper methods
        private Dictionary<string, string> ExtractHiddenFields(string html)
        {
            var fields = new Dictionary<string, string>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var hiddenInputs = doc.DocumentNode.SelectNodes("//input[@type='hidden']");
            if (hiddenInputs != null)
            {
                foreach (var input in hiddenInputs)
                {
                    string name = input.GetAttributeValue("name", "");
                    string value = input.GetAttributeValue("value", "");
                    if (!string.IsNullOrEmpty(name))
                    {
                        fields[name] = value;
                    }
                }
            }

            return fields;
        }

        private string GetTextById(HtmlAgilityPack.HtmlDocument doc, string id)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//input[@id='{id}']");
            return node?.GetAttributeValue("value", "") ?? "";
        }

        private int GetVietnameseDayOfWeek(DateTime date)
        {
            // Vietnamese: Thứ 2 = Monday, Thứ 3 = Tuesday, ..., Chủ nhật = Sunday (8)
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 6,
                DayOfWeek.Saturday => 7,
                DayOfWeek.Sunday => 8,
                _ => 0
            };
        }

        private (string start, string end) GetLessonTimes(List<int> lessons)
        {
            // Lesson time mapping (ACTVN schedule)
            // 1,2,3: 07:00 - 09:35
            // 4,5,6: 09:35 - 12:00
            // 7,8,9: 12:30 - 14:55
            // 10,11,12: 15:00 - 17:25
            
            var lessonTimeMap = new Dictionary<int, (string start, string end)>
            {
                {1, ("07:00", "07:50")},
                {2, ("07:50", "08:40")},
                {3, ("08:40", "09:35")},
                {4, ("09:35", "10:25")},
                {5, ("10:25", "11:15")},
                {6, ("11:15", "12:00")},
                {7, ("12:30", "13:20")},
                {8, ("13:20", "14:10")},
                {9, ("14:10", "14:55")},
                {10, ("15:00", "15:50")},
                {11, ("15:50", "16:40")},
                {12, ("16:40", "17:25")},
                {13, ("18:00", "18:50")},
                {14, ("18:50", "19:40")},
                {15, ("19:40", "20:30")}
            };

            if (lessons.Count == 0) return ("", "");
            
            int firstLesson = lessons.Min();
            int lastLesson = lessons.Max();
            
            string startTime = lessonTimeMap.ContainsKey(firstLesson) ? lessonTimeMap[firstLesson].start : "";
            string endTime = lessonTimeMap.ContainsKey(lastLesson) ? lessonTimeMap[lastLesson].end : "";
            
            return (startTime, endTime);
        }
    }

    // Models
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class StudentInfo
    {
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string Gender { get; set; }
        public string Birthday { get; set; }
        public string BankAccount { get; set; }
        public string IdCard { get; set; }
        public string BirthPlace { get; set; }
        public string PersonalPhone { get; set; }
        public string Email { get; set; }
        public string EmergencyContact { get; set; }
    }

    public class StudentScheduleResponse
    {
        public StudentInfo StudentInfo { get; set; }
        public List<CalendarEvent> Events { get; set; }
    }

    public class CalendarEvent
    {
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Teacher { get; set; }
        public string Location { get; set; }
        public string Date { get; set; }
        public List<int> Lessons { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int DayOfWeek { get; set; }
    }

    public class ActvnLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int RequestType { get; set; } = 2; // 2 = ACTVN Login
    }

    public class ActvnScheduleRequest
    {
        public string SessionCookie { get; set; }
        public int RequestType { get; set; } = 3; // 3 = ACTVN Schedule
    }
}
