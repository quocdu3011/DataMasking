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

            TransmissionLogger.LogServer("=======================================================");
            TransmissionLogger.LogServer($"[SERVER] Đã khởi động trên Port {port}");
            TransmissionLogger.LogServer($"[SERVER] RSA Key sẵn sàng (N: {serverKeyPair.N.ToString().Length} digits)");
            TransmissionLogger.LogServer("[SERVER] Đang chờ kết nối từ Client...");
            TransmissionLogger.LogServer("=======================================================");

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
                TransmissionLogger.LogServer($"[SERVER] Đã giải mã AES Key: {BitConverter.ToString(aesKey).Substring(0, 47)}...");

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

                // 6. Parse request với camelCase naming policy
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
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
    }

    public class StudentScoreRequest
    {
        public string Action { get; set; }
        public string StudentCode { get; set; }
        public int MaskingType { get; set; }
    }
}
