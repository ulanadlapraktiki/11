using Npgsql;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using WpfApp2.Models;

namespace WpfApp2
{
    public class AppDbContext
    {
        private string connString = "Host=localhost;Port=5432;Database=HraniteelPRO;Username=postgres;Password=postgres;Include Error Detail=true";

        public string HashMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ==================== РАБОТА С СОТРУДНИКАМИ ====================

        public bool Login(string login, string password)
        {
            string hash = HashMD5(password);
            string sql = "SELECT COUNT(*) FROM employees WHERE login=@login AND password_hash=@hash";

            try
            {
                using var conn = new NpgsqlConnection(connString);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("login", login);
                cmd.Parameters.AddWithValue("hash", hash);
                conn.Open();
                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool LoginExists(string login)
        {
            string sql = "SELECT COUNT(*) FROM employees WHERE login=@login";

            try
            {
                using var conn = new NpgsqlConnection(connString);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("login", login);
                conn.Open();
                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки логина: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public Guid GetEmployeeId(string login)
        {
            string sql = "SELECT id FROM employees WHERE login=@login";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login", login);
            conn.Open();
            return (Guid)cmd.ExecuteScalar();
        }

        public Employee GetEmployee(string login)
        {
            string sql = "SELECT id, login, password_hash, role FROM employees WHERE login=@login";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login", login);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new Employee
                {
                    Id = reader.GetGuid(0),
                    Login = reader.GetString(1),
                    PasswordHash = reader.GetString(2),
                    Role = reader.GetString(3)
                };
            }
            return null;
        }

        public string GetEmployeeRole(string login)
        {
            string sql = "SELECT role FROM employees WHERE login=@login";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login", login);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "user";
        }

        public bool Register(string login, string password, string role = "user")
        {
            if (LoginExists(login)) return false;

            string hash = HashMD5(password);
            string sql = "INSERT INTO employees (id, login, password_hash, role) VALUES (@id, @login, @hash, @role)";

            try
            {
                using var conn = new NpgsqlConnection(connString);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", Guid.NewGuid());
                cmd.Parameters.AddWithValue("login", login);
                cmd.Parameters.AddWithValue("hash", hash);
                cmd.Parameters.AddWithValue("role", role);
                conn.Open();
                int result = cmd.ExecuteNonQuery();
                return result > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== РАБОТА С ГОСТЯМИ ====================

        public Guid AddGuest(Guest guest)
        {
            string sql = @"INSERT INTO guests (id, last_name, first_name, middle_name, 
                           passport_series, passport_number, birth_date, phone, email, organization) 
                           VALUES (@id, @ln, @fn, @mn, @ps, @pn, @bd, @ph, @em, @org) RETURNING id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            Guid newId = Guid.NewGuid();
            cmd.Parameters.AddWithValue("id", newId);
            cmd.Parameters.AddWithValue("ln", guest.LastName);
            cmd.Parameters.AddWithValue("fn", guest.FirstName);
            cmd.Parameters.AddWithValue("mn", string.IsNullOrEmpty(guest.MiddleName) ? DBNull.Value : (object)guest.MiddleName);
            cmd.Parameters.AddWithValue("ps", string.IsNullOrEmpty(guest.PassportSeries) ? DBNull.Value : (object)guest.PassportSeries);
            cmd.Parameters.AddWithValue("pn", string.IsNullOrEmpty(guest.PassportNumber) ? DBNull.Value : (object)guest.PassportNumber);
            cmd.Parameters.AddWithValue("bd", guest.BirthDate);
            cmd.Parameters.AddWithValue("ph", string.IsNullOrEmpty(guest.Phone) ? DBNull.Value : (object)guest.Phone);
            cmd.Parameters.AddWithValue("em", guest.Email);
            cmd.Parameters.AddWithValue("org", string.IsNullOrEmpty(guest.Organization) ? DBNull.Value : (object)guest.Organization);
            conn.Open();
            cmd.ExecuteNonQuery();
            return newId;
        }

        public Guest GetGuest(Guid guestId)
        {
            string sql = "SELECT id, last_name, first_name, middle_name, passport_series, passport_number, birth_date, phone, email, organization FROM guests WHERE id=@id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", guestId);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new Guest
                {
                    Id = reader.GetGuid(0),
                    LastName = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    MiddleName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    PassportSeries = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PassportNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                    BirthDate = reader.GetDateTime(6),
                    Phone = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Email = reader.GetString(8),
                    Organization = reader.IsDBNull(9) ? null : reader.GetString(9)
                };
            }
            return null;
        }

        // ==================== РАБОТА С ЗАЯВКАМИ ====================

        public Guid CreateRequest(Request request)
        {
            string sql = @"INSERT INTO requests (id, guest_id, employee_id, start_date, end_date, 
                           visit_purpose, target_department, note, status) 
                           VALUES (@id, @gid, @eid, @start, @end, @purpose, @dept, @note, @status) RETURNING id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            Guid newId = Guid.NewGuid();
            cmd.Parameters.AddWithValue("id", newId);
            cmd.Parameters.AddWithValue("gid", request.GuestId);
            cmd.Parameters.AddWithValue("eid", request.EmployeeId);
            cmd.Parameters.AddWithValue("start", request.StartDate);
            cmd.Parameters.AddWithValue("end", request.EndDate);
            cmd.Parameters.AddWithValue("purpose", request.VisitPurpose ?? "");
            cmd.Parameters.AddWithValue("dept", request.TargetDepartment ?? "");
            cmd.Parameters.AddWithValue("note", request.Note ?? "");
            cmd.Parameters.AddWithValue("status", request.Status);
            conn.Open();
            cmd.ExecuteNonQuery();
            return newId;
        }

        public List<RequestInfo> GetRequestsByEmployee(Guid employeeId)
        {
            var list = new List<RequestInfo>();

            string sql = @"SELECT r.id, r.start_date, r.end_date, r.status,
                                  g.last_name, g.first_name, g.email, g.phone
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.employee_id = @eid
                           ORDER BY r.start_date DESC";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("eid", employeeId);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new RequestInfo
                {
                    Id = reader.GetGuid(0),
                    StartDate = reader.GetDateTime(1).ToShortDateString(),
                    EndDate = reader.GetDateTime(2).ToShortDateString(),
                    Status = reader.GetString(3),
                    GuestName = $"{reader.GetString(4)} {reader.GetString(5)}",
                    GuestEmail = reader.GetString(6),
                    GuestPhone = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }
            return list;
        }

        public RequestFull GetRequestById(Guid requestId)
        {
            string sql = @"SELECT r.id, r.start_date, r.end_date, r.status, r.visit_purpose, r.target_department, r.note,
                                  g.id, g.last_name, g.first_name, g.middle_name, 
                                  g.passport_series, g.passport_number, g.birth_date, 
                                  g.phone, g.email, g.organization
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.id = @id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new RequestFull
                {
                    Id = reader.GetGuid(0),
                    StartDate = reader.GetDateTime(1),
                    EndDate = reader.GetDateTime(2),
                    Status = reader.GetString(3),
                    VisitPurpose = reader.GetString(4),
                    TargetDepartment = reader.GetString(5),
                    Note = reader.GetString(6),
                    GuestId = reader.GetGuid(7),
                    LastName = reader.GetString(8),
                    FirstName = reader.GetString(9),
                    MiddleName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    PassportSeries = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    PassportNumber = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    BirthDate = reader.GetDateTime(13),
                    Phone = reader.IsDBNull(14) ? "" : reader.GetString(14),
                    Email = reader.GetString(15),
                    Organization = reader.IsDBNull(16) ? "" : reader.GetString(16)
                };
            }
            return null;
        }

        public bool UpdateRequestStatus(Guid requestId, string status)
        {
            string sql = "UPDATE requests SET status = @status WHERE id = @id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateRequest(RequestFull request)
        {
            string sql = @"UPDATE requests SET 
                           start_date = @start, 
                           end_date = @end, 
                           status = @status,
                           visit_purpose = @purpose,
                           target_department = @dept,
                           note = @note
                           WHERE id = @id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("start", request.StartDate);
            cmd.Parameters.AddWithValue("end", request.EndDate);
            cmd.Parameters.AddWithValue("status", request.Status);
            cmd.Parameters.AddWithValue("purpose", request.VisitPurpose);
            cmd.Parameters.AddWithValue("dept", request.TargetDepartment);
            cmd.Parameters.AddWithValue("note", request.Note);
            cmd.Parameters.AddWithValue("id", request.Id);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteRequest(Guid requestId)
        {
            string sql = "DELETE FROM requests WHERE id = @id";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        // ==================== РАБОТА С ФАЙЛАМИ ====================

        public bool AddAttachedFile(Guid requestId, string fileType, string filePath, string fileName)
        {
            string sql = @"INSERT INTO attached_files (id, request_id, file_type, file_path, file_name, uploaded_at) 
                           VALUES (@id, @rid, @type, @path, @name, @date)";

            try
            {
                using var conn = new NpgsqlConnection(connString);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", Guid.NewGuid());
                cmd.Parameters.AddWithValue("rid", requestId);
                cmd.Parameters.AddWithValue("type", fileType);
                cmd.Parameters.AddWithValue("path", filePath);
                cmd.Parameters.AddWithValue("name", fileName);
                cmd.Parameters.AddWithValue("date", DateTime.Now);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ==================== РАБОТА С ГРУППОВЫМИ ЗАЯВКАМИ ====================

        public bool AddGroupMember(Guid requestId, Guid guestId)
        {
            string sql = @"INSERT INTO group_members (id, request_id, guest_id) 
                           VALUES (@id, @rid, @gid)";

            try
            {
                using var conn = new NpgsqlConnection(connString);
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", Guid.NewGuid());
                cmd.Parameters.AddWithValue("rid", requestId);
                cmd.Parameters.AddWithValue("gid", guestId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления члена группы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}