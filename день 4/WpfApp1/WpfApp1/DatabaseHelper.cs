using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace WpfApp1
{
    public class DatabaseHelper
    {
        // ИЗМЕНИТЕ ПАРОЛЬ НА ВАШ!
        private string connString = "Host=localhost;Port=5432;Database=HraniteelPRO;Username=postgres;Password=postgres";

        // MD5 хэширование
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

        // Выполнение запроса с возвратом данных
        private DataTable Query(string sql, params NpgsqlParameter[] parameters)
        {
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            var dt = new DataTable();
            dt.Load(cmd.ExecuteReader());
            return dt;
        }

        // Выполнение запроса без возврата данных
        private int Execute(string sql, params NpgsqlParameter[] parameters)
        {
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        // ==================== АВТОРИЗАЦИЯ ====================

        public Employee GetEmployeeByCode(string code)
        {
            string sql = "SELECT id, code, login, role, department FROM employees WHERE code = @code";

            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new Employee
                {
                    Id = reader.GetGuid(0),
                    Code = reader.GetString(1),
                    Login = reader.GetString(2),
                    Role = reader.GetString(3),
                    Department = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
            }
            return null;
        }

        public bool EmployeeCodeExists(string code)
        {
            string sql = "SELECT COUNT(*) FROM employees WHERE code = @code";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("code", code);
            conn.Open();
            return (long)cmd.ExecuteScalar() > 0;
        }

        public bool TestConnection()
        {
            try
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        // ==================== ДЛЯ ОБЩЕГО ОТДЕЛА ====================

        public DataTable GetAllRequests()
        {
            string sql = @"SELECT r.id, r.requesttype, r.status, r.start_date, r.end_date, 
                                  r.visit_purpose, r.target_department, r.reject_reason,
                                  g.last_name, g.first_name, g.middle_name, g.email, g.phone, g.passport_number, g.birth_date, g.organization
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           ORDER BY r.created_at DESC";
            return Query(sql);
        }

        public DataRow GetRequestById(Guid requestId)
        {
            string sql = @"SELECT r.*, g.* 
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.id = @id";
            var dt = Query(sql, new NpgsqlParameter("id", requestId));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public DataTable GetAttachedFiles(Guid requestId)
        {
            string sql = "SELECT file_name, file_type, file_path FROM attached_files WHERE request_id = @rid";
            return Query(sql, new NpgsqlParameter("rid", requestId));
        }

        public bool IsInBlacklist(string lastName, string firstName, string passportNumber)
        {
            string sql = @"SELECT COUNT(*) FROM blacklist b
                           JOIN guests g ON b.guest_id = g.id
                           WHERE (g.last_name = @ln AND g.first_name = @fn) OR g.passport_number = @pn";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("ln", lastName);
            cmd.Parameters.AddWithValue("fn", firstName);
            cmd.Parameters.AddWithValue("pn", passportNumber);
            conn.Open();
            return (long)cmd.ExecuteScalar() > 0;
        }

        public bool UpdateRequestStatus(Guid requestId, string status, string rejectReason = "")
        {
            string sql = "UPDATE requests SET status = @status, reject_reason = @reason WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("reason", rejectReason);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<string> GetDepartments()
        {
            var list = new List<string>();
            var dt = Query("SELECT DISTINCT target_department FROM requests WHERE target_department IS NOT NULL");
            foreach (DataRow row in dt.Rows) list.Add(row[0].ToString());
            return list;
        }

        public List<string> GetStatuses()
        {
            return new List<string> { "проверка", "одобрена", "отклонена" };
        }

        public List<string> GetRequestTypes()
        {
            return new List<string> { "личная", "групповая" };
        }

        // ==================== ДЛЯ ОХРАНЫ ====================

        public DataTable GetApprovedRequests()
        {
            string sql = @"SELECT r.id, r.requesttype, r.start_date, r.end_date, 
                                  r.target_department, r.entry_time, r.exit_time,
                                  g.last_name, g.first_name, g.middle_name, g.passport_number, g.phone
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.status = 'одобрена'
                           ORDER BY r.start_date DESC";
            return Query(sql);
        }

        public DataTable GetApprovedRequestsByDate(DateTime date)
        {
            string sql = @"SELECT r.id, r.requesttype, r.start_date, r.end_date, 
                                  r.target_department, r.entry_time, r.exit_time,
                                  g.last_name, g.first_name, g.middle_name, g.passport_number, g.phone
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.status = 'одобрена' AND r.start_date <= @date AND r.end_date >= @date
                           ORDER BY r.start_date DESC";
            return Query(sql, new NpgsqlParameter("date", date));
        }

        public DataTable SearchApprovedRequests(string searchText)
        {
            string sql = @"SELECT r.id, r.requesttype, r.start_date, r.end_date, 
                                  r.target_department, r.entry_time, r.exit_time,
                                  g.last_name, g.first_name, g.middle_name, g.passport_number, g.phone
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.status = 'одобрена' 
                           AND (g.last_name ILIKE @search OR g.first_name ILIKE @search 
                                OR g.middle_name ILIKE @search OR g.passport_number ILIKE @search)
                           ORDER BY r.start_date DESC";
            return Query(sql, new NpgsqlParameter("search", $"%{searchText}%"));
        }

        public bool LogEntryTime(Guid requestId, DateTime entryTime)
        {
            string sql = "UPDATE requests SET entry_time = @time WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("time", entryTime);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool LogExitTime(Guid requestId, DateTime exitTime)
        {
            string sql = "UPDATE requests SET exit_time = @time WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("time", exitTime);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public DateTime? GetEntryTime(Guid requestId)
        {
            string sql = "SELECT entry_time FROM requests WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : (DateTime?)result;
        }

        public bool HasEntryTime(Guid requestId)
        {
            return GetEntryTime(requestId).HasValue;
        }

        public void SendGateOpenSignal(Guid requestId)
        {
            System.Diagnostics.Debug.WriteLine($"Сигнал на открытие турникета для заявки {requestId}");
        }

        public void PlaySystemSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        public List<string> GetApprovedDepartments()
        {
            var list = new List<string>();
            var dt = Query("SELECT DISTINCT target_department FROM requests WHERE status = 'одобрена' AND target_department IS NOT NULL");
            foreach (DataRow row in dt.Rows) list.Add(row[0].ToString());
            return list;
        }

        // ==================== ДЛЯ ПОДАЧИ ЗАЯВКИ ====================

        public Guid AddGuest(string lastName, string firstName, string middleName,
                             string passportSeries, string passportNumber,
                             DateTime birthDate, string phone, string email, string organization)
        {
            string sql = @"INSERT INTO guests (id, last_name, first_name, middle_name, 
                           passport_series, passport_number, birth_date, phone, email, organization) 
                           VALUES (@id, @ln, @fn, @mn, @ps, @pn, @bd, @ph, @em, @org) RETURNING id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            Guid newId = Guid.NewGuid();
            cmd.Parameters.AddWithValue("id", newId);
            cmd.Parameters.AddWithValue("ln", lastName);
            cmd.Parameters.AddWithValue("fn", firstName);
            cmd.Parameters.AddWithValue("mn", string.IsNullOrEmpty(middleName) ? DBNull.Value : (object)middleName);
            cmd.Parameters.AddWithValue("ps", string.IsNullOrEmpty(passportSeries) ? DBNull.Value : (object)passportSeries);
            cmd.Parameters.AddWithValue("pn", string.IsNullOrEmpty(passportNumber) ? DBNull.Value : (object)passportNumber);
            cmd.Parameters.AddWithValue("bd", birthDate);
            cmd.Parameters.AddWithValue("ph", string.IsNullOrEmpty(phone) ? DBNull.Value : (object)phone);
            cmd.Parameters.AddWithValue("em", email);
            cmd.Parameters.AddWithValue("org", string.IsNullOrEmpty(organization) ? DBNull.Value : (object)organization);
            conn.Open();
            cmd.ExecuteNonQuery();
            return newId;
        }

        public Guid CreateRequest(Guid guestId, Guid employeeId, DateTime startDate, DateTime endDate,
                                   string visitPurpose, string targetDepartment, string note)
        {
            string sql = @"INSERT INTO requests (id, guest_id, employee_id, start_date, end_date, 
                           visit_purpose, target_department, note, status) 
                           VALUES (@id, @gid, @eid, @start, @end, @purpose, @dept, @note, 'проверка') RETURNING id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            Guid newId = Guid.NewGuid();
            cmd.Parameters.AddWithValue("id", newId);
            cmd.Parameters.AddWithValue("gid", guestId);
            cmd.Parameters.AddWithValue("eid", employeeId);
            cmd.Parameters.AddWithValue("start", startDate);
            cmd.Parameters.AddWithValue("end", endDate);
            cmd.Parameters.AddWithValue("purpose", visitPurpose);
            cmd.Parameters.AddWithValue("dept", targetDepartment);
            cmd.Parameters.AddWithValue("note", note ?? "");
            conn.Open();
            cmd.ExecuteNonQuery();
            return newId;
        }

        public Guid GetGeneralDepartmentEmployeeId()
        {
            string sql = "SELECT id FROM employees WHERE role = 'general' LIMIT 1";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            conn.Open();
            return (Guid)cmd.ExecuteScalar();
        }

        // ==================== ДЛЯ ПОДРАЗДЕЛЕНИЯ ====================

        public DataTable GetRequestsByDepartment(string department)
        {
            string sql = @"SELECT r.id, r.requesttype, r.status, r.start_date, r.end_date, 
                                  r.visit_purpose, r.target_department, r.entry_time, r.exit_time, r.arrival_time,
                                  g.last_name, g.first_name, g.middle_name, g.passport_number, g.phone, g.email
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.target_department = @dept AND r.status = 'одобрена'
                           ORDER BY r.start_date DESC";
            return Query(sql, new NpgsqlParameter("dept", department));
        }

        public DataTable GetRequestsByDepartmentAndDate(string department, DateTime date)
        {
            string sql = @"SELECT r.id, r.requesttype, r.status, r.start_date, r.end_date, 
                                  r.visit_purpose, r.target_department, r.entry_time, r.exit_time, r.arrival_time,
                                  g.last_name, g.first_name, g.middle_name, g.passport_number, g.phone, g.email
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.target_department = @dept AND r.status = 'одобрена' 
                           AND r.start_date <= @date AND r.end_date >= @date
                           ORDER BY r.start_date DESC";
            return Query(sql, new NpgsqlParameter("dept", department), new NpgsqlParameter("date", date));
        }

        public DataTable GetVisitorsByRequest(Guid requestId)
        {
            string sql = @"SELECT g.id, g.last_name, g.first_name, g.middle_name, g.passport_number
                           FROM requests r
                           JOIN guests g ON r.guest_id = g.id
                           WHERE r.id = @rid
                           UNION
                           SELECT g.id, g.last_name, g.first_name, g.middle_name, g.passport_number
                           FROM group_members gm
                           JOIN guests g ON gm.guest_id = g.id
                           WHERE gm.request_id = @rid";
            return Query(sql, new NpgsqlParameter("rid", requestId));
        }

        public bool SetArrivalTime(Guid requestId, DateTime arrivalTime)
        {
            string sql = "UPDATE requests SET arrival_time = @time WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("time", arrivalTime);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public DateTime? GetArrivalTime(Guid requestId)
        {
            string sql = "SELECT arrival_time FROM requests WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : (DateTime?)result;
        }

        public bool HasArrivalTime(Guid requestId)
        {
            return GetArrivalTime(requestId).HasValue;
        }

        public DateTime? GetEntryTimeForRequest(Guid requestId)
        {
            string sql = "SELECT entry_time FROM requests WHERE id = @id";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", requestId);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : (DateTime?)result;
        }

        public void SendViolationNotification(Guid requestId, string message)
        {
            var request = GetRequestById(requestId);
            if (request != null)
            {
                string email = request["email"]?.ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    SendNotification(email, message);
                }
            }
            System.Diagnostics.Debug.WriteLine($"Нарушение по заявке {requestId}: {message}");
        }

        public void SendNotification(string email, string message)
        {
            System.Diagnostics.Debug.WriteLine($"Уведомление для {email}: {message}");
        }

        public int GetTravelTimeMinutes()
        {
            string sql = "SELECT travel_time_minutes FROM settings WHERE id = 1";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 30 : Convert.ToInt32(result);
        }

        public void AddToBlacklist(Guid guestId, string reason)
        {
            if (!IsGuestInBlacklist(guestId))
            {
                string sql = "INSERT INTO blacklist (id, guest_id, reason) VALUES (@id, @gid, @reason)";
                Execute(sql, new NpgsqlParameter("id", Guid.NewGuid()), new NpgsqlParameter("gid", guestId), new NpgsqlParameter("reason", reason));
            }
        }

        public bool IsGuestInBlacklist(Guid guestId)
        {
            string sql = "SELECT COUNT(*) FROM blacklist WHERE guest_id = @gid";
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("gid", guestId);
            conn.Open();
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    public class Employee
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Login { get; set; } = "";
        public string Role { get; set; } = "";
        public string? Department { get; set; }
    }
}