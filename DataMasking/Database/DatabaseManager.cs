using System;
using MySql.Data.MySqlClient;
using System.Data;

namespace DataMasking.Database
{
    public class DatabaseManager
    {
        private string connectionString;
        private MySqlConnection connection;

        public DatabaseManager(string server = "localhost", string database = "datamasking_db", 
                               string user = "root", string password = "", int port = 3306)
        {
            connectionString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};";
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

        public void InitializeDatabase()
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS sensitive_data (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        full_name VARCHAR(100),
                        email VARCHAR(100),
                        phone VARCHAR(20),
                        credit_card VARCHAR(19),
                        ssn VARCHAR(11),
                        address TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";

                using (MySqlCommand cmd = new MySqlCommand(createTableQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // Tạo bảng lưu dữ liệu đã mã hóa
                string createEncryptedTableQuery = @"
                    CREATE TABLE IF NOT EXISTS encrypted_data (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        original_id INT,
                        encrypted_content LONGTEXT,
                        encryption_type VARCHAR(20),
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (original_id) REFERENCES sensitive_data(id) ON DELETE CASCADE
                    )";

                using (MySqlCommand cmd = new MySqlCommand(createEncryptedTableQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khởi tạo database: " + ex.Message);
            }
        }

        public int InsertSensitiveData(string fullName, string email, string phone, 
                                       string creditCard, string ssn, string address)
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = @"INSERT INTO sensitive_data 
                    (full_name, email, phone, credit_card, ssn, address) 
                    VALUES (@fullName, @email, @phone, @creditCard, @ssn, @address);
                    SELECT LAST_INSERT_ID();";

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@fullName", fullName);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    cmd.Parameters.AddWithValue("@creditCard", creditCard);
                    cmd.Parameters.AddWithValue("@ssn", ssn);
                    cmd.Parameters.AddWithValue("@address", address);

                    int id = Convert.ToInt32(cmd.ExecuteScalar());
                    connection.Close();
                    return id;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi thêm dữ liệu: " + ex.Message);
            }
        }

        public void InsertEncryptedData(int originalId, string encryptedContent, string encryptionType)
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = @"INSERT INTO encrypted_data 
                    (original_id, encrypted_content, encryption_type) 
                    VALUES (@originalId, @encryptedContent, @encryptionType)";

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@originalId", originalId);
                    cmd.Parameters.AddWithValue("@encryptedContent", encryptedContent);
                    cmd.Parameters.AddWithValue("@encryptionType", encryptionType);

                    cmd.ExecuteNonQuery();
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi lưu dữ liệu mã hóa: " + ex.Message);
            }
        }

        public DataTable GetAllSensitiveData()
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "SELECT * FROM sensitive_data ORDER BY id DESC";
                MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                connection.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi lấy dữ liệu: " + ex.Message);
            }
        }

        public DataTable GetAllEncryptedData()
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = @"SELECT e.id, e.original_id, s.full_name, 
                    e.encrypted_content, e.encryption_type, e.created_at 
                    FROM encrypted_data e 
                    LEFT JOIN sensitive_data s ON e.original_id = s.id 
                    ORDER BY e.id DESC";

                MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                connection.Close();
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi lấy dữ liệu mã hóa: " + ex.Message);
            }
        }

        public void DeleteSensitiveData(int id)
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "DELETE FROM sensitive_data WHERE id = @id";
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi xóa dữ liệu: " + ex.Message);
            }
        }

        public void InsertSampleData()
        {
            try
            {
                InsertSensitiveData("Nguyễn Văn An", "nguyenvanan@email.com", "0901234567", 
                    "4532-1234-5678-9010", "123-45-6789", "123 Đường Lê Lợi, Q1, TP.HCM");
                
                InsertSensitiveData("Trần Thị Bình", "tranthibinh@email.com", "0912345678", 
                    "5425-2345-6789-0123", "234-56-7890", "456 Đường Nguyễn Huệ, Q1, TP.HCM");
                
                InsertSensitiveData("Lê Văn Cường", "levancuong@email.com", "0923456789", 
                    "4916-3456-7890-1234", "345-67-8901", "789 Đường Hai Bà Trưng, Q3, TP.HCM");
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi thêm dữ liệu mẫu: " + ex.Message);
            }
        }
    }
}
