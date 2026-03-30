using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using DataMasking.Models;

namespace DataMasking.Database
{
    public class StudentDatabaseManager
    {
        private string connectionString;

        public StudentDatabaseManager(string server = "36.50.54.109", string database = "kmalegend", 
                                     string user = "anonymous", string password = "Thuanld@255", int port = 3306)
        {
            connectionString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};CharSet=utf8;SslMode=None;AllowPublicKeyRetrieval=True;";
        }

        public bool TestConnection()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public Student GetStudentByCode(string studentCode)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // Query đơn giản, chính xác theo schema
                    string query = "SELECT student_id, student_code, student_name, student_class FROM students WHERE student_code = @code";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", studentCode.Trim());
                        
                        // Log query để debug
                        System.Diagnostics.Debug.WriteLine($"[DB] Executing query: {query}");
                        System.Diagnostics.Debug.WriteLine($"[DB] Parameter @code = '{studentCode.Trim()}'");
                        
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                System.Diagnostics.Debug.WriteLine($"[DB] Found student: {reader.GetString("student_name")}");
                                return new Student
                                {
                                    StudentId = reader.GetInt64("student_id"),
                                    StudentCode = reader.GetString("student_code"),
                                    StudentName = reader.GetString("student_name"),
                                    StudentClass = reader.IsDBNull(reader.GetOrdinal("student_class")) ? "" : reader.GetString("student_class")
                                };
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[DB] No student found with code: '{studentCode.Trim()}'");
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DB] StackTrace: {ex.StackTrace}");
                throw new Exception("Lỗi truy vấn sinh viên: " + ex.Message + " | Query: SELECT FROM students WHERE student_code = '" + studentCode + "'");
            }
        }

        public List<ScoreDetail> GetScoresByStudentId(long studentId)
        {
            List<ScoreDetail> scores = new List<ScoreDetail>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.id, s.student_id, s.subject_id, s.score_text, s.score_first, 
                               s.score_second, s.score_final, s.score_over_rall, s.semester,
                               sub.subject_name, sub.subject_credits
                        FROM scores s
                        INNER JOIN subjects sub ON s.subject_id = sub.subject_id
                        WHERE s.student_id = @studentId
                        ORDER BY s.semester DESC, sub.subject_name";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentId", studentId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                scores.Add(new ScoreDetail
                                {
                                    Id = reader.GetInt64("id"),
                                    StudentId = reader.GetInt64("student_id"),
                                    SubjectId = reader.GetInt64("subject_id"),
                                    ScoreText = reader.IsDBNull(reader.GetOrdinal("score_text")) ? "" : reader.GetString("score_text"),
                                    ScoreFirst = reader.GetFloat("score_first"),
                                    ScoreSecond = reader.GetFloat("score_second"),
                                    ScoreFinal = reader.GetFloat("score_final"),
                                    ScoreOverall = reader.GetFloat("score_over_rall"),
                                    Semester = reader.IsDBNull(reader.GetOrdinal("semester")) ? "" : reader.GetString("semester"),
                                    SubjectName = reader.GetString("subject_name"),
                                    SubjectCredits = reader.GetInt64("subject_credits")
                                });
                            }
                        }
                    }
                }
                return scores;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi truy vấn điểm: " + ex.Message);
            }
        }

        public string GetLastSemester(long studentId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT semester FROM scores WHERE student_id = @studentId ORDER BY semester DESC LIMIT 1";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentId", studentId);
                        object result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        public List<string> GetAllStudentCodes(int limit = 10)
        {
            List<string> codes = new List<string>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT student_code FROM students LIMIT {limit}";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                codes.Add(reader.GetString("student_code"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi lấy danh sách mã SV: " + ex.Message);
            }
            return codes;
        }

        public int GetTotalStudents()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM students";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
    }

    public class ScoreDetail
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public long SubjectId { get; set; }
        public string ScoreText { get; set; }
        public float ScoreFirst { get; set; }
        public float ScoreSecond { get; set; }
        public float ScoreFinal { get; set; }
        public float ScoreOverall { get; set; }
        public string Semester { get; set; }
        public string SubjectName { get; set; }
        public long SubjectCredits { get; set; }
    }
}
