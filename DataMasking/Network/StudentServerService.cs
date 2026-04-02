using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DataMasking.Crypto;
using DataMasking.Key;
using DataMasking.Database;
using DataMasking.Masking;
using DataMasking.Models;

namespace DataMasking.Network
{
    public class StudentServerService
    {
        private TcpListener listener;
        private RSAKeyPair serverKeyPair;
        private StudentDatabaseManager dbManager;
        private MaskingService maskingService;
        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;

        public StudentServerService(RSAKeyPair keyPair, StudentDatabaseManager dbManager)
        {
            this.serverKeyPair = keyPair;
            this.dbManager = dbManager;
            this.maskingService = new MaskingService();
            
            // Log để verify dbManager
            TransmissionLogger.LogServer($"[SERVER] StudentServerService initialized with dbManager");
        }

        public async Task Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            TransmissionLogger.LogServer($"[SERVER] Đang chờ kết nối trên Port {port}...");

            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client), cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        TransmissionLogger.LogServer($"[SERVER] Lỗi accept client: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            
            try
            {
                TransmissionLogger.LogServer("=======================================================");
                TransmissionLogger.LogServer($"[SERVER] Client kết nối từ: {clientEndpoint}");

                NetworkStream stream = client.GetStream();

                // 1. Nhận encrypted AES key
                byte[] keyLengthBytes = new byte[4];
                await stream.ReadAsync(keyLengthBytes, 0, 4);
                int keyLength = BitConverter.ToInt32(keyLengthBytes, 0);

                byte[] encryptedAESKey = new byte[keyLength];
                await stream.ReadAsync(encryptedAESKey, 0, keyLength);
                TransmissionLogger.LogServer($"[SERVER] Đã nhận Encrypted AES Key ({keyLength} bytes)");

                // 2. Giải mã AES key bằng RSA private key
                RSA rsa = new RSA(serverKeyPair.N, serverKeyPair.E, serverKeyPair.D);
                byte[] aesKey = rsa.Decrypt(encryptedAESKey);
                
                // Validate key length
                if (aesKey.Length != 16 && aesKey.Length != 24 && aesKey.Length != 32)
                {
                    TransmissionLogger.LogServer($"[SERVER] CẢNH BÁO: AES Key length không hợp lệ: {aesKey.Length} bytes");
                    TransmissionLogger.LogServer($"[SERVER] Full AES Key (Hex): {BitConverter.ToString(aesKey)}");
                    
                    // Nếu key dài hơn 32 bytes, cắt về 32 bytes
                    if (aesKey.Length > 32)
                    {
                        byte[] trimmedKey = new byte[32];
                        Array.Copy(aesKey, trimmedKey, 32);
                        aesKey = trimmedKey;
                        TransmissionLogger.LogServer($"[SERVER] Đã trim key về 32 bytes");
                    }
                    // Nếu key ngắn hơn 16 bytes, pad thêm
                    else if (aesKey.Length < 16)
                    {
                        byte[] paddedKey = new byte[16];
                        Array.Copy(aesKey, paddedKey, aesKey.Length);
                        aesKey = paddedKey;
                        TransmissionLogger.LogServer($"[SERVER] Đã pad key lên 16 bytes");
                    }
                }
                
                TransmissionLogger.LogServer($"[SERVER] Đã giải mã AES Key ({aesKey.Length} bytes): {BitConverter.ToString(aesKey).Substring(0, Math.Min(47, BitConverter.ToString(aesKey).Length))}...");

                // 3. Nhận IV
                byte[] iv = new byte[16];
                await stream.ReadAsync(iv, 0, 16);
                TransmissionLogger.LogServer($"[SERVER] Đã nhận IV ({iv.Length} bytes)");

                // 4. Nhận encrypted request
                byte[] requestLengthBytes = new byte[4];
                await stream.ReadAsync(requestLengthBytes, 0, 4);
                int requestLength = BitConverter.ToInt32(requestLengthBytes, 0);

                byte[] encryptedRequest = new byte[requestLength];
                int totalRead = 0;
                while (totalRead < requestLength)
                {
                    int read = await stream.ReadAsync(encryptedRequest, totalRead, requestLength - totalRead);
                    totalRead += read;
                }
                string encryptedRequestHex = BitConverter.ToString(encryptedRequest).Replace("-", "");
                TransmissionLogger.LogServer($"[SERVER] Đã nhận Encrypted Request ({requestLength} bytes)");
                TransmissionLogger.LogServer($"[SERVER] Encrypted Request (Hex): {encryptedRequestHex.Substring(0, Math.Min(100, encryptedRequestHex.Length))}...");

                // 5. Giải mã request
                AES aes = new AES(aesKey);
                byte[] decryptedRequest = aes.DecryptCBC(encryptedRequest, iv);
                string requestJson = Encoding.UTF8.GetString(decryptedRequest);
                TransmissionLogger.LogServer($"[SERVER] Đã giải mã request");
                TransmissionLogger.LogServer($"[SERVER] Request JSON: {requestJson}");

                // 6. Parse request - check action type first
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Determine request type by checking "action" field
                using (var jsonDoc = System.Text.Json.JsonDocument.Parse(requestJson))
                {
                    string action = jsonDoc.RootElement.TryGetProperty("action", out var actionProp) 
                        ? actionProp.GetString() ?? "score_lookup" 
                        : "score_lookup";

                    TransmissionLogger.LogServer($"[SERVER] Request action: {action}");

                    if (action == "actvn_login")
                    {
                        await HandleActvnLogin(requestJson, stream, aes, iv);
                        return;
                    }
                    else if (action == "actvn_schedule")
                    {
                        await HandleActvnSchedule(requestJson, stream, aes, iv);
                        return;
                    }
                    else if (action == "actvn_profile")
                    {
                        await HandleActvnProfile(requestJson, stream, aes, iv);
                        return;
                    }
                    else if (action == "virtual_scores")
                    {
                        await HandleVirtualScores(requestJson, stream, aes, iv);
                        return;
                    }
                    else if (action == "save_virtual_scores")
                    {
                        await HandleSaveVirtualScores(requestJson, stream, aes, iv);
                        return;
                    }
                }

                // Default: Score lookup request
                var request = System.Text.Json.JsonSerializer.Deserialize<StudentScoreRequest>(requestJson, jsonOptions);
                
                if (request == null)
                {
                    TransmissionLogger.LogServer($"[SERVER] Request JSON parse failed - null result");
                    return;
                }
                
                if (string.IsNullOrEmpty(request.StudentCode))
                {
                    TransmissionLogger.LogServer($"[SERVER] StudentCode is null or empty in request");
                    TransmissionLogger.LogServer($"[SERVER] Full request object: Action={request.Action}, MaskingType={request.MaskingType}");
                    
                    // Send error response
                    var errorResponse = new StudentScoreResponse
                    {
                        Success = false,
                        Message = "Mã sinh viên không được để trống!"
                    };
                    
                    string errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                    
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorJson);
                    byte[] encryptedError = aes.EncryptCBC(errorBytes, iv);
                    
                    byte[] errorLengthBytes = BitConverter.GetBytes(encryptedError.Length);
                    await stream.WriteAsync(errorLengthBytes, 0, 4);
                    await stream.WriteAsync(encryptedError, 0, encryptedError.Length);
                    return;
                }
                
                TransmissionLogger.LogServer($"[SERVER] Xử lý tra cứu cho mã SV: {request.StudentCode}");
                TransmissionLogger.LogServer($"[SERVER] Masking type: {request.MaskingType}");

                // 7. Xử lý request
                var response = ProcessScoreLookup(request.StudentCode, request.MaskingType);

                // 8. Serialize response
                string responseJson = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                if (response.Success)
                {
                    TransmissionLogger.LogServer($"[SERVER] Tra cứu thành công! Tìm thấy {response.Scores?.Count ?? 0} môn học");
                    TransmissionLogger.LogServer($"[SERVER] GPA: {response.GPA:F2} | CPA: {response.CPA:F2} | Tín chỉ: {response.TotalCredits}");
                }
                else
                {
                    TransmissionLogger.LogServer($"[SERVER] Tra cứu thất bại: {response.Message}");
                }

                // 9. Mã hóa response
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                byte[] encryptedResponse = aes.EncryptCBC(responseBytes, iv);
                string encryptedResponseHex = BitConverter.ToString(encryptedResponse).Replace("-", "");
                TransmissionLogger.LogServer($"[SERVER] Đã mã hóa response ({encryptedResponse.Length} bytes)");
                TransmissionLogger.LogServer($"[SERVER] Encrypted Response (Hex): {encryptedResponseHex.Substring(0, Math.Min(100, encryptedResponseHex.Length))}...");

                // 10. Gửi response
                byte[] responseLengthBytes = BitConverter.GetBytes(encryptedResponse.Length);
                await stream.WriteAsync(responseLengthBytes, 0, 4);
                await stream.WriteAsync(encryptedResponse, 0, encryptedResponse.Length);
                TransmissionLogger.LogServer($"[SERVER] Đã gửi Encrypted Response ({encryptedResponse.Length} bytes)");
                TransmissionLogger.LogServer($"[SERVER] Hoàn thành xử lý request từ {clientEndpoint}");
                TransmissionLogger.LogServer("═══════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Lỗi xử lý client {clientEndpoint}: {ex.Message}");
                TransmissionLogger.LogServer("═══════════════════════════════════════════════════════");
            }
            finally
            {
                client.Close();
            }
        }

        private StudentScoreResponse ProcessScoreLookup(string studentCode, int maskingType)
        {
            try
            {
                // Null/empty check
                if (string.IsNullOrWhiteSpace(studentCode))
                {
                    TransmissionLogger.LogServer($"[SERVER] StudentCode is null or empty");
                    return new StudentScoreResponse
                    {
                        Success = false,
                        Message = "Mã sinh viên không được để trống!"
                    };
                }
                
                TransmissionLogger.LogServer($"[SERVER] Đang tìm sinh viên với mã: '{studentCode}'");
                TransmissionLogger.LogServer($"[SERVER] Length: {studentCode.Length}, Trimmed: '{studentCode.Trim()}', Length after trim: {studentCode.Trim().Length}");
                
                // Test connection first
                if (!dbManager.TestConnection())
                {
                    TransmissionLogger.LogServer($"[SERVER] Không thể kết nối database!");
                    return new StudentScoreResponse
                    {
                        Success = false,
                        Message = "Lỗi kết nối database!"
                    };
                }
                TransmissionLogger.LogServer($"[SERVER] Database connection OK");
                
                // Get student
                Student student = dbManager.GetStudentByCode(studentCode);
                if (student == null)
                {
                    TransmissionLogger.LogServer($"[SERVER] Không tìm thấy sinh viên với mã: '{studentCode}'");
                    
                    // Try to get some sample codes for debugging
                    try
                    {
                        var samples = dbManager.GetAllStudentCodes(3);
                        TransmissionLogger.LogServer($"[SERVER] Sample codes in DB: {string.Join(", ", samples)}");
                    }
                    catch { }
                    
                    return new StudentScoreResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy sinh viên với mã này!"
                    };
                }

                TransmissionLogger.LogServer($"[SERVER] Tìm thấy sinh viên: {student.StudentName} (ID: {student.StudentId})");

                // Apply masking
                MaskingType maskType = (MaskingType)maskingType;
                string maskedName = maskingService.ApplyMasking(student.StudentName, "name", maskType);
                // Masking lớp: hiển thị chữ cái đầu, mask phần sau
                string maskedClass = MaskClassCode(student.StudentClass);

                TransmissionLogger.LogServer($"[SERVER] Đã mask: {student.StudentName} → {maskedName}");

                // Get scores
                List<ScoreDetail> scores = dbManager.GetScoresByStudentId(student.StudentId);
                TransmissionLogger.LogServer($"[SERVER] Tìm thấy {scores.Count} môn học");
                
                string lastSemester = dbManager.GetLastSemester(student.StudentId);
                TransmissionLogger.LogServer($"[SERVER] Học kỳ cuối: {lastSemester}");

                // Calculate GPA, CPA
                var (gpa, cpa, totalCredits) = CalculateGPAandCPA(scores, lastSemester);

                // Convert to response format
                var scoreInfos = scores.Select(s => new ScoreInfo
                {
                    Semester = s.Semester,
                    SubjectName = s.SubjectName,
                    SubjectCredits = s.SubjectCredits,
                    ScoreFirst = s.ScoreFirst,
                    ScoreSecond = s.ScoreSecond,
                    ScoreFinal = s.ScoreFinal,
                    ScoreOverall = s.ScoreOverall,
                    ScoreText = ConvertToLetterGrade(s.ScoreOverall),
                    IsPassed = IsPassedSubject(s.ScoreFinal, s.ScoreOverall)
                }).ToList();

                return new StudentScoreResponse
                {
                    Success = true,
                    Message = "Tra cứu thành công",
                    Student = new StudentInfo
                    {
                        StudentCode = student.StudentCode,
                        StudentName = maskedName,
                        StudentClass = maskedClass
                    },
                    Scores = scoreInfos,
                    GPA = gpa,
                    CPA = cpa,
                    TotalCredits = totalCredits,
                    LastSemester = lastSemester
                };
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Lỗi xử lý: {ex.Message}");
                TransmissionLogger.LogServer($"[SERVER] StackTrace: {ex.StackTrace}");
                return new StudentScoreResponse
                {
                    Success = false,
                    Message = "Lỗi xử lý: " + ex.Message
                };
            }
        }

        private (double gpa, double cpa, long totalCredits) CalculateGPAandCPA(List<ScoreDetail> allScores, string lastSemester)
        {
            // GPA - current semester only, exclude PE, only passed subjects
            var currentSemesterScores = allScores
                .Where(s => s.Semester == lastSemester && !IsPhysicalEducation(s.SubjectName) && IsPassedSubject(s.ScoreFinal, s.ScoreOverall))
                .ToList();

            double gpa = 0;
            if (currentSemesterScores.Count > 0)
            {
                double totalGradePoints = 0;
                long totalCredits = 0;

                foreach (var score in currentSemesterScores)
                {
                    double gradePoint = ConvertScoreToGradePoint(score.ScoreOverall);
                    totalGradePoints += gradePoint * score.SubjectCredits;
                    totalCredits += score.SubjectCredits;
                }

                gpa = totalCredits > 0 ? totalGradePoints / totalCredits : 0;
            }

            // CPA - all semesters, exclude PE, only passed subjects
            var cpaScores = allScores
                .Where(s => !IsPhysicalEducation(s.SubjectName) && IsPassedSubject(s.ScoreFinal, s.ScoreOverall))
                .ToList();

            double cpa = 0;
            if (cpaScores.Count > 0)
            {
                double totalGradePoints = 0;
                long totalCredits = 0;

                foreach (var score in cpaScores)
                {
                    double gradePoint = ConvertScoreToGradePoint(score.ScoreOverall);
                    totalGradePoints += gradePoint * score.SubjectCredits;
                    totalCredits += score.SubjectCredits;
                }

                cpa = totalCredits > 0 ? totalGradePoints / totalCredits : 0;
            }

            // Total completed credits (including PE, only passed)
            long completedCredits = allScores
                .Where(s => IsPassedSubject(s.ScoreFinal, s.ScoreOverall))
                .Sum(s => s.SubjectCredits);

            return (gpa, cpa, completedCredits);
        }

        private bool IsPhysicalEducation(string subjectName)
        {
            string[] peKeywords = { "thể chất", "thể dục", "giáo dục thể chất", "gdtc", "physical education", "pe" };
            string lowerName = subjectName.ToLower();
            return peKeywords.Any(keyword => lowerName.Contains(keyword));
        }

        private string MaskClassCode(string classCode)
        {
            if (string.IsNullOrEmpty(classCode) || classCode.Length <= 1)
                return classCode;
            
            // Hiển thị chữ cái đầu, mask phần sau
            return classCode[0] + new string('*', classCode.Length - 1);
        }

        private bool IsPassedSubject(float scoreFinal, float scoreOverall)
        {
            // Điểm cuối kì < 2 => trượt
            if (scoreFinal < 2.0f)
                return false;

            // Điểm cuối kì >= 2 và điểm tổng >= 4 => qua
            if (scoreFinal >= 2.0f && scoreOverall >= 4.0f)
                return true;

            // Các trường hợp khác => trượt
            return false;
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

        private string ConvertToLetterGrade(float score)
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

        public void Stop()
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();
            listener?.Stop();
            TransmissionLogger.LogServer("[SERVER] Đã dừng");
        }

        private async Task HandleActvnProfile(string requestJson, System.Net.Sockets.NetworkStream stream, AES aes, byte[] iv)
        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = System.Text.Json.JsonSerializer.Deserialize<ActvnLoginRequest>(requestJson, jsonOptions);

                TransmissionLogger.LogServer($"[SERVER] ACTVN Profile request - Username: {request.Username}");

                var crawler = new Services.ActvnCrawlerService();
                var loginResult = await crawler.LoginAsync(request.Username, request.Password);

                ActvnProfileResponse response;
                if (loginResult.Success)
                {
                    var info = await crawler.FetchStudentProfileAsync();
                    response = new ActvnProfileResponse
                    {
                        Success = true,
                        Message = "Lấy thông tin thành công",
                        StudentInfo = new ActvnStudentInfo
                        {
                            StudentCode   = info.StudentCode,
                            StudentName   = info.StudentName,
                            Gender        = info.Gender,
                            Birthday      = info.Birthday,
                            BankAccount   = info.BankAccount,
                            IdCard        = info.IdCard,
                            BirthPlace    = info.BirthPlace,
                            PersonalPhone = info.PersonalPhone,
                            Email         = info.Email,
                            EmergencyContact = info.EmergencyContact
                        }
                    };
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Profile OK - {info.StudentName}");
                }
                else
                {
                    response = new ActvnProfileResponse { Success = false, Message = loginResult.Message };
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Profile login failed: {loginResult.Message}");
                }

                await SendEncryptedResponse(response, stream, aes, iv);
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Error in HandleActvnProfile: {ex.Message}");
                await SendEncryptedResponse(new ActvnProfileResponse { Success = false, Message = ex.Message }, stream, aes, iv);
            }
        }

        private async Task HandleActvnLogin(string requestJson, System.Net.Sockets.NetworkStream stream, AES aes, byte[] iv)        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = System.Text.Json.JsonSerializer.Deserialize<ActvnLoginRequest>(requestJson, jsonOptions);

                TransmissionLogger.LogServer($"[SERVER] ACTVN Login request - Username: {request.Username}");

                var crawler = new Services.ActvnCrawlerService();
                var loginResult = await crawler.LoginAsync(request.Username, request.Password);

                ActvnLoginResponse response;
                if (loginResult.Success)
                {
                    var studentInfo = await crawler.FetchStudentProfileAsync();
                    response = new ActvnLoginResponse
                    {
                        Success = true,
                        Message = "Đăng nhập thành công",
                        StudentInfo = new ActvnStudentInfo
                        {
                            StudentCode      = studentInfo.StudentCode,
                            StudentName      = studentInfo.StudentName,
                            Gender           = studentInfo.Gender,
                            Birthday         = studentInfo.Birthday,
                            BankAccount      = studentInfo.BankAccount,
                            IdCard           = studentInfo.IdCard,
                            BirthPlace       = studentInfo.BirthPlace,
                            PersonalPhone    = studentInfo.PersonalPhone,
                            Email            = studentInfo.Email,
                            EmergencyContact = studentInfo.EmergencyContact
                        }
                    };
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Login successful - Student: {studentInfo.StudentName} ({studentInfo.StudentCode})");
                }
                else
                {
                    response = new ActvnLoginResponse
                    {
                        Success = false,
                        Message = loginResult.Message
                    };
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Login failed: {loginResult.Message}");
                }

                await SendEncryptedResponse(response, stream, aes, iv);
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Error in HandleActvnLogin: {ex.Message}");
                var errorResponse = new ActvnLoginResponse { Success = false, Message = $"Lỗi: {ex.Message}" };
                await SendEncryptedResponse(errorResponse, stream, aes, iv);
            }
        }

        private async Task HandleActvnSchedule(string requestJson, System.Net.Sockets.NetworkStream stream, AES aes, byte[] iv)
        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = System.Text.Json.JsonSerializer.Deserialize<ActvnScheduleRequest>(requestJson, jsonOptions);

                TransmissionLogger.LogServer($"[SERVER] ACTVN Schedule request - Username: {request.Username}");

                var crawler = new Services.ActvnCrawlerService();
                var loginResult = await crawler.LoginAsync(request.Username, request.Password);

                ActvnScheduleResponse response;
                if (loginResult.Success)
                {
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Login successful, fetching schedule...");
                    var scheduleData = await crawler.FetchStudentScheduleAsync();
                    
                    TransmissionLogger.LogServer($"[SERVER] Schedule fetched - Student: {scheduleData.StudentInfo.StudentName}");
                    TransmissionLogger.LogServer($"[SERVER] Events count: {scheduleData.Events.Count}");
                    
                    if (scheduleData.Events.Count > 0)
                    {
                        TransmissionLogger.LogServer($"[SERVER] Sample event: {scheduleData.Events[0].Title} on {scheduleData.Events[0].Date}");
                    }
                    
                    response = new ActvnScheduleResponse
                    {
                        Success = true,
                        Message = "Lấy lịch học thành công",
                        StudentInfo = new ActvnStudentInfo
                        {
                            StudentCode = scheduleData.StudentInfo.StudentCode,
                            StudentName = scheduleData.StudentInfo.StudentName,
                            Gender = scheduleData.StudentInfo.Gender,
                            Birthday = scheduleData.StudentInfo.Birthday
                        },
                        Events = scheduleData.Events.Select(e => new ActvnCalendarEvent
                        {
                            Title = e.Title,
                            CourseCode = e.CourseCode,
                            Teacher = e.Teacher,
                            Location = e.Location,
                            Date = e.Date,
                            Lessons = e.Lessons,
                            StartTime = e.StartTime,
                            EndTime = e.EndTime,
                            DayOfWeek = e.DayOfWeek
                        }).ToList()
                    };
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Schedule successful - {response.Events.Count} events found");
                }
                else
                {
                    response = new ActvnScheduleResponse
                    {
                        Success = false,
                        Message = loginResult.Message,
                        Events = new List<ActvnCalendarEvent>()
                    };
                    TransmissionLogger.LogServer($"[SERVER] ACTVN Schedule failed: {loginResult.Message}");
                }

                await SendEncryptedResponse(response, stream, aes, iv);
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Error in HandleActvnSchedule: {ex.Message}");
                TransmissionLogger.LogServer($"[SERVER] Stack trace: {ex.StackTrace}");
                var errorResponse = new ActvnScheduleResponse 
                { 
                    Success = false, 
                    Message = $"Lỗi: {ex.Message}",
                    Events = new List<ActvnCalendarEvent>()
                };
                await SendEncryptedResponse(errorResponse, stream, aes, iv);
            }
        }

        private async Task SendEncryptedResponse<T>(T response, System.Net.Sockets.NetworkStream stream, AES aes, byte[] iv)
        {
            string responseJson = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
            byte[] encryptedResponse = aes.EncryptCBC(responseBytes, iv);
            
            TransmissionLogger.LogServer($"[SERVER] Sending encrypted response ({encryptedResponse.Length} bytes)");

            byte[] responseLengthBytes = BitConverter.GetBytes(encryptedResponse.Length);
            await stream.WriteAsync(responseLengthBytes, 0, 4);
            await stream.WriteAsync(encryptedResponse, 0, encryptedResponse.Length);
        }

        private async Task HandleVirtualScores(string requestJson, System.Net.Sockets.NetworkStream stream, AES aes, byte[] iv)
        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = System.Text.Json.JsonSerializer.Deserialize<VirtualScoreRequest>(requestJson, jsonOptions);

                TransmissionLogger.LogServer($"[SERVER] Virtual Scores request - Student Code: {request.StudentCode}");

                VirtualScoreResponse response;

                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(
                    "Server=36.50.54.109;Port=3306;Database=kmalegend;Uid=anonymous;Pwd=Thuanld@255;CharSet=utf8;SslMode=None;AllowPublicKeyRetrieval=True;"))
                {
                    conn.Open();

                    // Check if batch exists for this student
                    string getBatchQuery = @"SELECT batch_id FROM score_batches 
                        WHERE student_code = @code 
                        ORDER BY last_updated DESC LIMIT 1";

                    long batchId = 0;
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(getBatchQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", request.StudentCode);
                        var result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            TransmissionLogger.LogServer($"[SERVER] No virtual scores found for student: {request.StudentCode}");
                            response = new VirtualScoreResponse
                            {
                                Success = true,
                                Message = "Chưa có dữ liệu bảng điểm ảo",
                                Items = new List<VirtualScoreItemDto>()
                            };
                            await SendEncryptedResponse(response, stream, aes, iv);
                            return;
                        }

                        batchId = Convert.ToInt64(result);
                        TransmissionLogger.LogServer($"[SERVER] Found batch_id: {batchId}");
                    }

                    // Get all items for this batch
                    string getItemsQuery = @"SELECT subject_name, subject_credit, score_first, score_second, 
                        score_final, score_overall, score_text, is_selected 
                        FROM score_items WHERE batch_id = @batchId";

                    var items = new List<VirtualScoreItemDto>();
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(getItemsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@batchId", batchId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new VirtualScoreItemDto
                                {
                                    SubjectName = reader.IsDBNull(reader.GetOrdinal("subject_name")) ? "" : reader.GetString("subject_name"),
                                    SubjectCredit = reader.IsDBNull(reader.GetOrdinal("subject_credit")) ? 0 : reader.GetInt64("subject_credit"),
                                    ScoreFirst = reader.IsDBNull(reader.GetOrdinal("score_first")) ? 0f : reader.GetFloat("score_first"),
                                    ScoreSecond = reader.IsDBNull(reader.GetOrdinal("score_second")) ? 0f : reader.GetFloat("score_second"),
                                    ScoreFinal = reader.IsDBNull(reader.GetOrdinal("score_final")) ? 0f : reader.GetFloat("score_final"),
                                    ScoreOverall = reader.IsDBNull(reader.GetOrdinal("score_overall")) ? 0f : reader.GetFloat("score_overall"),
                                    ScoreText = reader.IsDBNull(reader.GetOrdinal("score_text")) ? "" : reader.GetString("score_text"),
                                    IsSelected = reader.IsDBNull(reader.GetOrdinal("is_selected")) ? false : reader.GetBoolean("is_selected")
                                });
                            }
                        }
                    }

                    TransmissionLogger.LogServer($"[SERVER] Loaded {items.Count} virtual score items");

                    response = new VirtualScoreResponse
                    {
                        Success = true,
                        Message = "Lấy dữ liệu thành công",
                        Items = items
                    };
                }

                await SendEncryptedResponse(response, stream, aes, iv);
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Error in HandleVirtualScores: {ex.Message}");
                TransmissionLogger.LogServer($"[SERVER] Stack trace: {ex.StackTrace}");
                var errorResponse = new VirtualScoreResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}",
                    Items = new List<VirtualScoreItemDto>()
                };
                await SendEncryptedResponse(errorResponse, stream, aes, iv);
            }
        }

        private async Task HandleSaveVirtualScores(string requestJson, System.Net.Sockets.NetworkStream stream, AES aes, byte[] iv)
        {
            try
            {
                TransmissionLogger.LogServer($"[SERVER] Save Virtual Scores request");
                TransmissionLogger.LogServer($"[SERVER] Request JSON: {requestJson.Substring(0, Math.Min(200, requestJson.Length))}...");

                var request = System.Text.Json.JsonSerializer.Deserialize<SaveVirtualScoreRequest>(requestJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                TransmissionLogger.LogServer($"[SERVER] Save request - Student Code: {request.StudentCode}, Scores count: {request.Scores?.Count ?? 0}");

                bool success = false;
                string message = "";

                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(
                    "Server=36.50.54.109;Port=3306;Database=kmalegend;Uid=anonymous;Pwd=Thuanld@255;CharSet=utf8;SslMode=None;AllowPublicKeyRetrieval=True;"))
                {
                    conn.Open();

                    // Get student info
                    var student = dbManager.GetStudentByCode(request.StudentCode);
                    if (student == null)
                    {
                        TransmissionLogger.LogServer($"[SERVER] Student not found: {request.StudentCode}");
                        message = "Không tìm thấy sinh viên";
                    }
                    else
                    {
                        // Start transaction
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // 1. Delete all old score_items for this student
                                string deleteItemsQuery = @"DELETE si FROM score_items si 
                                    INNER JOIN score_batches sb ON si.batch_id = sb.batch_id 
                                    WHERE sb.student_code = @code";
                                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(deleteItemsQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@code", request.StudentCode);
                                    int deletedItems = cmd.ExecuteNonQuery();
                                    TransmissionLogger.LogServer($"[SERVER] Deleted {deletedItems} old score items");
                                }

                                // 2. Delete old batch if exists
                                string deleteBatchQuery = "DELETE FROM score_batches WHERE student_code = @code";
                                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(deleteBatchQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@code", request.StudentCode);
                                    int deletedBatches = cmd.ExecuteNonQuery();
                                    TransmissionLogger.LogServer($"[SERVER] Deleted {deletedBatches} old batches");
                                }

                                // 3. Insert new batch
                                string insertBatchQuery = @"INSERT INTO score_batches 
                                    (student_code, student_name, student_class, last_updated) 
                                    VALUES (@code, @name, @class, @updated)";

                                long batchId;
                                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(insertBatchQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@code", student.StudentCode);
                                    cmd.Parameters.AddWithValue("@name", student.StudentName);
                                    cmd.Parameters.AddWithValue("@class", student.StudentClass);
                                    cmd.Parameters.AddWithValue("@updated", DateTime.Now);
                                    cmd.ExecuteNonQuery();
                                    batchId = cmd.LastInsertedId;
                                    TransmissionLogger.LogServer($"[SERVER] Created new batch with ID: {batchId}");
                                }

                                // 4. Insert all new items
                                string insertItemQuery = @"INSERT INTO score_items 
                                    (batch_id, subject_name, subject_credit, score_first, score_second, 
                                     score_final, score_overall, score_text, is_selected) 
                                    VALUES (@batchId, @name, @credit, @first, @second, @final, @overall, @text, @selected)";

                                int insertedCount = 0;
                                foreach (var score in request.Scores)
                                {
                                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(insertItemQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@batchId", batchId);
                                        cmd.Parameters.AddWithValue("@name", score.SubjectName);
                                        cmd.Parameters.AddWithValue("@credit", score.SubjectCredit);
                                        cmd.Parameters.AddWithValue("@first", score.ScoreFirst);
                                        cmd.Parameters.AddWithValue("@second", score.ScoreSecond);
                                        cmd.Parameters.AddWithValue("@final", score.ScoreFinal);
                                        cmd.Parameters.AddWithValue("@overall", score.ScoreOverall);
                                        cmd.Parameters.AddWithValue("@text", score.ScoreText);
                                        cmd.Parameters.AddWithValue("@selected", score.IsSelected);
                                        cmd.ExecuteNonQuery();
                                        insertedCount++;
                                    }
                                }

                                // Commit transaction
                                transaction.Commit();
                                TransmissionLogger.LogServer($"[SERVER] Successfully saved {insertedCount} score items");
                                success = true;
                                message = $"Đã lưu {insertedCount} môn học thành công";
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                TransmissionLogger.LogServer($"[SERVER] Transaction rolled back: {ex.Message}");
                                message = $"Lỗi lưu dữ liệu: {ex.Message}";
                            }
                        }
                    }
                }

                var response = new
                {
                    success = success,
                    message = message
                };

                await SendEncryptedResponse(response, stream, aes, iv);
                TransmissionLogger.LogServer($"[SERVER] Save response sent: Success={success}");
            }
            catch (Exception ex)
            {
                TransmissionLogger.LogServer($"[SERVER] Error in HandleSaveVirtualScores: {ex.Message}");
                TransmissionLogger.LogServer($"[SERVER] Stack trace: {ex.StackTrace}");
                var errorResponse = new
                {
                    success = false,
                    message = $"Lỗi server: {ex.Message}"
                };
                await SendEncryptedResponse(errorResponse, stream, aes, iv);
            }
        }
    }

    public class StudentScoreRequest
    {
        public string Action { get; set; }
        public string StudentCode { get; set; }
        public int MaskingType { get; set; }
    }

    public class VirtualScoreRequest
    {
        public string Action { get; set; }
        public string StudentCode { get; set; }
    }

    public class SaveVirtualScoreRequest
    {
        public string Action { get; set; }
        public string StudentCode { get; set; }
        public List<SaveVirtualScoreItemDto> Scores { get; set; }
    }

    public class SaveVirtualScoreItemDto
    {
        public string SubjectName { get; set; }
        public long SubjectCredit { get; set; }
        public float ScoreFirst { get; set; }
        public float ScoreSecond { get; set; }
        public float ScoreFinal { get; set; }
        public float ScoreOverall { get; set; }
        public string ScoreText { get; set; }
        public bool IsSelected { get; set; }
    }
}
