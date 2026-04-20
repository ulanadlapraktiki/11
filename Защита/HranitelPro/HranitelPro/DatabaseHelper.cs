using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace HranitelPro
{
    public class DatabaseHelper
    {
        private string connString = "Host=localhost;Port=5432;Database=HraniteelPRO;Username=postgres;Password=postgres;Include Error Detail=true";

        // ==================== ХЭШИРОВАНИЕ ====================
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

        // Для охраны
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

        public List<string> GetApprovedDepartments()
        {
            var list = new List<string>();
            var dt = Query("SELECT DISTINCT target_department FROM requests WHERE status = 'одобрена' AND target_department IS NOT NULL");
            foreach (DataRow row in dt.Rows) list.Add(row[0].ToString());
            return list;
        }

        public bool HasEntryTime(Guid requestId)
        {
            var dt = Query("SELECT entry_time FROM requests WHERE id = @id", new NpgsqlParameter("id", requestId));
            return dt.Rows[0][0] != DBNull.Value;
        }

        public void LogEntryTime(Guid requestId, DateTime time)
        {
            Execute("UPDATE requests SET entry_time = @time WHERE id = @id",
                new NpgsqlParameter("time", time),
                new NpgsqlParameter("id", requestId));
        }

        public void LogExitTime(Guid requestId, DateTime time)
        {
            Execute("UPDATE requests SET exit_time = @time WHERE id = @id",
                new NpgsqlParameter("time", time),
                new NpgsqlParameter("id", requestId));
        }

        public void PlaySystemSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        public void SendGateOpenSignal(Guid requestId)
        {
            System.Diagnostics.Debug.WriteLine($"Сигнал на открытие турникета для заявки {requestId}");
        }

        public List<string> GetRequestTypes()
        {
            return new List<string> { "личная", "групповая" };
        }

        // ==================== ОТЧЕТ ЗА ПОСЛЕДНИЕ 3 ЧАСА ====================

        public DataTable GetLast3HoursReport()
        {
            DateTime endTime = DateTime.Now;
            DateTime startTime = endTime.AddHours(-3);

            string sql = @"SELECT 
                    COALESCE(r.targetdepartment, 'Не указано') AS department,
                    COUNT(*) AS count
                   FROM visitrequests r
                   WHERE r.status = 'одобрена' 
                     AND r.entry_time >= @startTime 
                     AND r.entry_time <= @endTime
                   GROUP BY r.targetdepartment
                   ORDER BY count DESC";

            return Query(sql,
                new NpgsqlParameter("startTime", startTime),
                new NpgsqlParameter("endTime", endTime));
        }

        public void SaveLast3HoursReport()
        {
            try
            {
                DateTime now = DateTime.Now;
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folderPath = Path.Combine(documentsPath, "Отчеты ТБ");
                string todayFolder = Path.Combine(folderPath, now.ToString("dd_MM_yyyy"));

                if (!Directory.Exists(todayFolder))
                {
                    Directory.CreateDirectory(todayFolder);
                }

                int hour = now.Hour;
                int periodStart = ((hour - 3) / 3) * 3;
                if (periodStart < 0) periodStart = 0;
                string timePeriod = $"{periodStart:00}:00-{hour:00}:00";

                string fileName = Path.Combine(todayFolder, $"Отчет_за_{timePeriod}.csv");

                var data = GetLast3HoursReport();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Отчет за последние 3 часа: {timePeriod}");
                sb.AppendLine($"Дата: {now:dd.MM.yyyy}");
                sb.AppendLine($"Время формирования: {now:HH:mm:ss}");
                sb.AppendLine("");
                sb.AppendLine("Подразделение;Количество посетителей");

                int total = 0;
                foreach (DataRow row in data.Rows)
                {
                    int count = Convert.ToInt32(row["count"]);
                    sb.AppendLine($"{row["department"]};{count}");
                    total += count;
                }

                sb.AppendLine($"\nИТОГО за период: {total}");

                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения отчета: {ex.Message}");
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================
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

        private int Execute(string sql, params NpgsqlParameter[] parameters)
        {
            using var conn = new NpgsqlConnection(connString);
            using var cmd = new NpgsqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        private DateTime ConvertToDateTime(object value)
        {
            if (value is DateTime dt) return dt;
            if (value is DateOnly dateOnly) return dateOnly.ToDateTime(TimeOnly.MinValue);
            if (value is string str) return DateTime.Parse(str);
            return Convert.ToDateTime(value);
        }

        // ==================== АВТОРИЗАЦИЯ ПОСЕТИТЕЛЯ ====================
        public User? LoginORM(string login, string hash)
        {
            var dt = Query("SELECT userid, lastname, firstname, patronymic, login FROM users WHERE login=@l AND passwordhash=@p",
                new NpgsqlParameter("l", login), new NpgsqlParameter("p", hash));
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new User
            {
                UserID = Convert.ToInt32(row["userid"]),
                LastName = row["lastname"]?.ToString() ?? "",
                FirstName = row["firstname"]?.ToString() ?? "",
                Patronymic = row["patronymic"]?.ToString(),
                Login = row["login"]?.ToString() ?? ""
            };
        }

        public bool LoginExists(string login)
        {
            var dt = Query("SELECT COUNT(*) FROM users WHERE login=@l", new NpgsqlParameter("l", login));
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        // ==================== РЕГИСТРАЦИЯ ====================
        public bool RegisterSQL(string last, string first, string? patron, string? phone, string email,
                          DateTime birth, string passport, string login, string hash)
        {
            string sql = @"INSERT INTO users (lastname, firstname, patronymic, phone, email, birthdate, passportdata, login, passwordhash) 
                           VALUES (@ln, @fn, @pat, @ph, @em, @bd, @pd, @l, @pwd)";
            return Execute(sql,
                new NpgsqlParameter("ln", last),
                new NpgsqlParameter("fn", first),
                new NpgsqlParameter("pat", string.IsNullOrEmpty(patron) ? DBNull.Value : (object)patron),
                new NpgsqlParameter("ph", string.IsNullOrEmpty(phone) ? DBNull.Value : (object)phone),
                new NpgsqlParameter("em", email),
                new NpgsqlParameter("bd", birth),
                new NpgsqlParameter("pd", passport),
                new NpgsqlParameter("l", login),
                new NpgsqlParameter("pwd", hash)) > 0;
        }

        // ==================== РАБОТА С СОТРУДНИКАМИ ====================
        public Employee? GetEmployeeByCode(string code)
        {
            var dt = Query("SELECT id, code, login, role, department FROM employees WHERE code = @code",
                new NpgsqlParameter("code", code));
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Employee
            {
                Id = (Guid)row["id"],
                Code = row["code"]?.ToString() ?? "",
                Login = row["login"]?.ToString() ?? "",
                Role = row["role"]?.ToString() ?? "",
                Department = row["department"]?.ToString()
            };
        }

        public bool EmployeeCodeExists(string code)
        {
            var dt = Query("SELECT COUNT(*) FROM employees WHERE code = @code", new NpgsqlParameter("code", code));
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        public DataTable GetAllEmployees()
        {
            return Query("SELECT id, code, login, role, department FROM employees ORDER BY login");
        }

        // ==================== ЗАЯВКИ ====================
        public List<RequestItem> GetUserRequests(int userId)
        {
            var list = new List<RequestItem>();
            var dt = Query(@"SELECT requestid, requesttype, status, startdate, enddate, visitpurpose, targetdepartment, createdat
                            FROM visitrequests WHERE userid=@uid ORDER BY createdat DESC",
                            new NpgsqlParameter("uid", userId));
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new RequestItem
                {
                    Id = Convert.ToInt32(row["requestid"]),
                    Type = row["requesttype"]?.ToString() ?? "",
                    Status = row["status"]?.ToString() ?? "",
                    StartDate = ConvertToDateTime(row["startdate"]).ToShortDateString(),
                    EndDate = ConvertToDateTime(row["enddate"]).ToShortDateString(),
                    Purpose = row["visitpurpose"]?.ToString() ?? "",
                    Department = row["targetdepartment"]?.ToString() ?? "",
                    CreatedAt = ConvertToDateTime(row["createdat"]).ToShortDateString()
                });
            }
            return list;
        }

        public DataRow? GetRequestByIdAsDataRow(int requestId)
        {
            var dt = Query(@"SELECT requestid, requesttype, status, startdate, enddate, visitpurpose, targetdepartment, note,
                           visitor_lastname, visitor_firstname, visitor_patronymic, visitor_phone, visitor_email,
                           visitor_organization, visitor_birthdate, visitor_passportdata
                    FROM visitrequests WHERE requestid=@id",
                            new NpgsqlParameter("id", requestId));
            if (dt.Rows.Count == 0) return null;
            return dt.Rows[0];
        }

        public RequestFull? GetRequestById(int requestId)
        {
            var dt = Query(@"SELECT requestid, requesttype, status, startdate, enddate, visitpurpose, targetdepartment, note,
                           visitor_lastname, visitor_firstname, visitor_patronymic, visitor_phone, visitor_email,
                           visitor_organization, visitor_birthdate, visitor_passportdata
                    FROM visitrequests WHERE requestid=@id",
                            new NpgsqlParameter("id", requestId));
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new RequestFull
            {
                RequestID = Convert.ToInt32(row["requestid"]),
                RequestType = row["requesttype"]?.ToString() ?? "",
                Status = row["status"]?.ToString() ?? "",
                StartDate = ConvertToDateTime(row["startdate"]),
                EndDate = ConvertToDateTime(row["enddate"]),
                VisitPurpose = row["visitpurpose"]?.ToString() ?? "",
                TargetDepartment = row["targetdepartment"]?.ToString() ?? "",
                Note = row["note"]?.ToString() ?? "",
                VisitorLastName = row["visitor_lastname"]?.ToString() ?? "",
                VisitorFirstName = row["visitor_firstname"]?.ToString() ?? "",
                VisitorPatronymic = row["visitor_patronymic"]?.ToString(),
                VisitorPhone = row["visitor_phone"]?.ToString(),
                VisitorEmail = row["visitor_email"]?.ToString() ?? "",
                VisitorOrganization = row["visitor_organization"]?.ToString(),
                VisitorBirthDate = row["visitor_birthdate"] == DBNull.Value ? DateTime.Now : ConvertToDateTime(row["visitor_birthdate"]),
                VisitorPassportData = row["visitor_passportdata"]?.ToString() ?? ""
            };
        }

        public int CreateRequest(int userId, DateTime start, DateTime end, string purpose, string dept, int empId, string note,
                          string lastName, string firstName, string? patronymic, string? phone, string email,
                          string? organization, DateTime birthDate, string passportData)
        {
            string sql = @"INSERT INTO visitrequests (userid, requesttype, status, startdate, enddate, 
                   visitpurpose, targetdepartment, targetemployeeid, note,
                   visitor_lastname, visitor_firstname, visitor_patronymic, visitor_phone, visitor_email,
                   visitor_organization, visitor_birthdate, visitor_passportdata)
                   VALUES (@uid, 'личная', 'проверка', @start, @end, @purpose, @dept, @emp, @note,
                           @ln, @fn, @pat, @ph, @em, @org, @bd, @pd) RETURNING requestid";

            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("start", start);
            cmd.Parameters.AddWithValue("end", end);
            cmd.Parameters.AddWithValue("purpose", purpose);
            cmd.Parameters.AddWithValue("dept", dept);
            cmd.Parameters.AddWithValue("emp", empId);
            cmd.Parameters.AddWithValue("note", note);
            cmd.Parameters.AddWithValue("ln", lastName);
            cmd.Parameters.AddWithValue("fn", firstName);
            cmd.Parameters.AddWithValue("pat", string.IsNullOrEmpty(patronymic) ? DBNull.Value : (object)patronymic);
            cmd.Parameters.AddWithValue("ph", string.IsNullOrEmpty(phone) ? DBNull.Value : (object)phone);
            cmd.Parameters.AddWithValue("em", email);
            cmd.Parameters.AddWithValue("org", string.IsNullOrEmpty(organization) ? DBNull.Value : (object)organization);
            cmd.Parameters.AddWithValue("bd", birthDate);
            cmd.Parameters.AddWithValue("pd", passportData);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int UpdateRequest(int requestId, DateTime start, DateTime end, string purpose, string dept, int empId, string note,
                          string lastName, string firstName, string? patronymic, string? phone, string email,
                          string? organization, DateTime birthDate, string passportData)
        {
            string sql = @"UPDATE visitrequests SET 
                   startdate=@start, enddate=@end, visitpurpose=@purpose, 
                   targetdepartment=@dept, targetemployeeid=@emp, note=@note,
                   visitor_lastname=@ln, visitor_firstname=@fn, visitor_patronymic=@pat,
                   visitor_phone=@ph, visitor_email=@em, visitor_organization=@org,
                   visitor_birthdate=@bd, visitor_passportdata=@pd
                   WHERE requestid=@id";

            Execute(sql,
                new NpgsqlParameter("start", start),
                new NpgsqlParameter("end", end),
                new NpgsqlParameter("purpose", purpose),
                new NpgsqlParameter("dept", dept),
                new NpgsqlParameter("emp", empId),
                new NpgsqlParameter("note", note),
                new NpgsqlParameter("ln", lastName),
                new NpgsqlParameter("fn", firstName),
                new NpgsqlParameter("pat", string.IsNullOrEmpty(patronymic) ? DBNull.Value : (object)patronymic),
                new NpgsqlParameter("ph", string.IsNullOrEmpty(phone) ? DBNull.Value : (object)phone),
                new NpgsqlParameter("em", email),
                new NpgsqlParameter("org", string.IsNullOrEmpty(organization) ? DBNull.Value : (object)organization),
                new NpgsqlParameter("bd", birthDate),
                new NpgsqlParameter("pd", passportData),
                new NpgsqlParameter("id", requestId));

            return requestId;
        }

        public int DeleteRequest(int requestId)
        {
            return Execute("DELETE FROM visitrequests WHERE requestid=@id", new NpgsqlParameter("id", requestId));
        }

        public bool UpdateRequestStatus(int requestId, string status)
        {
            return Execute("UPDATE visitrequests SET status = @status WHERE requestid = @id",
                new NpgsqlParameter("status", status),
                new NpgsqlParameter("id", requestId)) > 0;
        }

        public bool UpdateVisitDateTime(int requestId, DateTime startDate, DateTime endDate)
        {
            return Execute("UPDATE visitrequests SET startdate = @start, enddate = @end WHERE requestid = @id",
                new NpgsqlParameter("start", startDate),
                new NpgsqlParameter("end", endDate),
                new NpgsqlParameter("id", requestId)) > 0;
        }

        // ==================== ВСЕ ЗАЯВКИ ДЛЯ ОБЩЕГО ОТДЕЛА ====================
        public DataTable GetAllRequests()
        {
            return Query(@"SELECT r.requestid, r.requesttype, r.status, r.startdate, r.enddate, 
                                  r.visitpurpose, r.targetdepartment, r.note,
                                  r.visitor_lastname, r.visitor_firstname, r.visitor_patronymic, 
                                  r.visitor_phone, r.visitor_email,
                                  u.lastname AS user_lastname, u.firstname AS user_firstname
                           FROM visitrequests r
                           LEFT JOIN users u ON r.userid = u.userid
                           ORDER BY r.createdat DESC");
        }

        // ==================== СПРАВОЧНИКИ ====================
        public DataTable GetDepartments()
        {
            return Query("SELECT DISTINCT department FROM employees WHERE department IS NOT NULL ORDER BY department");
        }

        public DataTable GetEmployees(string department)
        {
            return Query(@"SELECT id as employeeid, login as fullname FROM employees WHERE department=@dept ORDER BY login",
                           new NpgsqlParameter("dept", department));
        }

        // ==================== ФАЙЛЫ ====================
        public List<AttachedFile> GetAttachedFiles(int requestId)
        {
            var list = new List<AttachedFile>();
            var dt = Query("SELECT fileid, filetype, filepath, filename FROM attachedfiles WHERE requestid = @id",
                new NpgsqlParameter("id", requestId));
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AttachedFile
                {
                    FileId = Convert.ToInt32(row["fileid"]),
                    FileType = row["filetype"]?.ToString() ?? "",
                    FilePath = row["filepath"]?.ToString() ?? "",
                    FileName = row["filename"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public int AddAttachedFile(int requestId, string fileType, string filePath, string fileName)
        {
            string sql = @"INSERT INTO attachedfiles (requestid, filetype, filepath, filename) 
                   VALUES (@rid, @type, @path, @name)";
            return Execute(sql,
                new NpgsqlParameter("rid", requestId),
                new NpgsqlParameter("type", fileType),
                new NpgsqlParameter("path", filePath),
                new NpgsqlParameter("name", fileName));
        }

        public int DeleteAttachedFile(int fileId)
        {
            return Execute("DELETE FROM attachedfiles WHERE fileid = @id", new NpgsqlParameter("id", fileId));
        }

        public int DeleteAllAttachedFiles(int requestId)
        {
            return Execute("DELETE FROM attachedfiles WHERE requestid = @rid", new NpgsqlParameter("rid", requestId));
        }

        // ==================== ЧЕРНЫЙ СПИСОК ====================
        public bool IsInBlacklist(string lastName, string firstName, string passportNumber)
        {
            try
            {
                var dt = Query(@"SELECT COUNT(*) FROM blacklist b
                               JOIN guests g ON b.guest_id = g.id
                               WHERE g.last_name = @ln AND g.first_name = @fn",
                               new NpgsqlParameter("ln", lastName),
                               new NpgsqlParameter("fn", firstName));
                return Convert.ToInt32(dt.Rows[0][0]) > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool AddToBlacklist(Guid guestId, string reason)
        {
            string sql = "INSERT INTO blacklist (id, guest_id, reason) VALUES (@id, @gid, @reason)";
            return Execute(sql,
                new NpgsqlParameter("id", Guid.NewGuid()),
                new NpgsqlParameter("gid", guestId),
                new NpgsqlParameter("reason", reason)) > 0;
        }

        public DataTable GetBlacklist()
        {
            return Query(@"SELECT b.id, b.reason, b.created_at, 
                                  g.last_name, g.first_name, g.middle_name, g.passport_number
                           FROM blacklist b
                           JOIN guests g ON b.guest_id = g.id
                           ORDER BY b.created_at DESC");
        }

        // ==================== ГОСТИ ====================
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

        public Guest? GetGuestById(Guid guestId)
        {
            var dt = Query("SELECT * FROM guests WHERE id = @id", new NpgsqlParameter("id", guestId));
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];
            return new Guest
            {
                Id = (Guid)row["id"],
                LastName = row["last_name"]?.ToString() ?? "",
                FirstName = row["first_name"]?.ToString() ?? "",
                MiddleName = row["middle_name"]?.ToString(),
                PassportSeries = row["passport_series"]?.ToString(),
                PassportNumber = row["passport_number"]?.ToString(),
                BirthDate = Convert.ToDateTime(row["birth_date"]),
                Phone = row["phone"]?.ToString(),
                Email = row["email"]?.ToString() ?? "",
                Organization = row["organization"]?.ToString()
            };
        }

        // ==================== ГРУППОВЫЕ ЗАЯВКИ ====================
        public bool AddGroupMember(Guid requestId, Guid guestId)
        {
            string sql = "INSERT INTO group_members (id, request_id, guest_id) VALUES (@id, @rid, @gid)";
            return Execute(sql,
                new NpgsqlParameter("id", Guid.NewGuid()),
                new NpgsqlParameter("rid", requestId),
                new NpgsqlParameter("gid", guestId)) > 0;
        }

        public DataTable GetGroupMembers(Guid requestId)
        {
            return Query(@"SELECT g.id, g.last_name, g.first_name, g.middle_name, g.passport_number, g.phone, g.email
                           FROM group_members gm
                           JOIN guests g ON gm.guest_id = g.id
                           WHERE gm.request_id = @rid",
                           new NpgsqlParameter("rid", requestId));
        }

        // ==================== ЛОГИ ДОСТУПА ====================
        public bool AddAccessLog(Guid requestId, string accessType)
        {
            string sql = "INSERT INTO access_logs (request_id, access_time, access_type) VALUES (@rid, @time, @type)";
            return Execute(sql,
                new NpgsqlParameter("rid", requestId),
                new NpgsqlParameter("time", DateTime.Now),
                new NpgsqlParameter("type", accessType)) > 0;
        }

        public DataTable GetAccessLogs(Guid requestId)
        {
            return Query("SELECT * FROM access_logs WHERE request_id = @rid ORDER BY access_time DESC",
                new NpgsqlParameter("rid", requestId));
        }

        // ==================== МЕТОДЫ ДЛЯ ОТЧЕТОВ ====================

        public DataTable GetReportByDay(DateTime date)
        {
            string sql = @"SELECT 
                            COALESCE(r.targetdepartment, 'Не указано') AS department,
                            COUNT(*) AS count
                           FROM visitrequests r
                           WHERE r.status = 'одобрена' 
                             AND r.startdate <= @date 
                             AND r.enddate >= @date
                           GROUP BY r.targetdepartment
                           ORDER BY count DESC";

            return Query(sql, new NpgsqlParameter("date", date));
        }

        public DataTable GetReportByMonth(DateTime date)
        {
            DateTime startDate = new DateTime(date.Year, date.Month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            string sql = @"SELECT 
                            COALESCE(r.targetdepartment, 'Не указано') AS department,
                            COUNT(*) AS count
                           FROM visitrequests r
                           WHERE r.status = 'одобрена' 
                             AND r.startdate <= @endDate 
                             AND r.enddate >= @startDate
                           GROUP BY r.targetdepartment
                           ORDER BY count DESC";

            return Query(sql,
                new NpgsqlParameter("startDate", startDate),
                new NpgsqlParameter("endDate", endDate));
        }

        public DataTable GetReportByYear(DateTime date)
        {
            DateTime startDate = new DateTime(date.Year, 1, 1);
            DateTime endDate = new DateTime(date.Year, 12, 31);

            string sql = @"SELECT 
                            COALESCE(r.targetdepartment, 'Не указано') AS department,
                            COUNT(*) AS count
                           FROM visitrequests r
                           WHERE r.status = 'одобрена' 
                             AND r.startdate <= @endDate 
                             AND r.enddate >= @startDate
                           GROUP BY r.targetdepartment
                           ORDER BY count DESC";

            return Query(sql,
                new NpgsqlParameter("startDate", startDate),
                new NpgsqlParameter("endDate", endDate));
        }

        public DataTable GetCurrentVisitors()
        {
            string sql = @"SELECT 
                            COALESCE(r.targetdepartment, 'Не указано') AS department,
                            r.visitor_lastname AS last_name,
                            r.visitor_firstname AS first_name,
                            COALESCE(r.visitor_patronymic, '') AS middle_name,
                            r.entry_time,
                            r.status
                           FROM visitrequests r
                           WHERE r.status = 'одобрена' 
                             AND r.entry_time IS NOT NULL 
                             AND r.exit_time IS NULL
                           ORDER BY r.targetdepartment, r.entry_time DESC";

            return Query(sql);
        }

        public void SaveAutoReport()
        {
            try
            {
                DateTime now = DateTime.Now;
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folderPath = Path.Combine(documentsPath, "Отчеты ТБ");
                string todayFolder = Path.Combine(folderPath, now.ToString("dd_MM_yyyy"));

                // Создаем папку, если её нет
                if (!Directory.Exists(todayFolder))
                {
                    Directory.CreateDirectory(todayFolder);
                }

                int hour = now.Hour;
                int period = (hour / 3) * 3;
                string timePeriod = $"{period:00}:00-{(period + 3):00}:00";

                string fileName = Path.Combine(todayFolder, $"Отчет_за_{timePeriod}.csv");

                var data = GetReportForTimePeriod(now.Date, period);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Отчет за период: {timePeriod}");
                sb.AppendLine($"Дата: {now:dd.MM.yyyy}");
                sb.AppendLine($"Время формирования: {now:HH:mm:ss}");
                sb.AppendLine("");
                sb.AppendLine("Подразделение;Количество посетителей");

                foreach (DataRow row in data.Rows)
                {
                    sb.AppendLine($"{row["department"]};{row["count"]}");
                }

                int total = 0;
                foreach (DataRow row in data.Rows)
                {
                    total += Convert.ToInt32(row["count"]);
                }
                sb.AppendLine($"\nИТОГО за период: {total}");

                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка автоотчета: {ex.Message}");
            }
        }
        private DataTable GetReportForTimePeriod(DateTime date, int startHour)
        {
            DateTime startTime = date.AddHours(startHour);
            DateTime endTime = date.AddHours(startHour + 3);

            string sql = @"SELECT 
                            COALESCE(r.targetdepartment, 'Не указано') AS department,
                            COUNT(*) AS count
                           FROM visitrequests r
                           WHERE r.status = 'одобрена' 
                             AND r.entry_time >= @startTime 
                             AND r.entry_time < @endTime
                           GROUP BY r.targetdepartment
                           ORDER BY count DESC";

            return Query(sql,
                new NpgsqlParameter("startTime", startTime),
                new NpgsqlParameter("endTime", endTime));
        }
    }

    // ==================== МОДЕЛИ ====================

    public class User
    {
        public int UserID { get; set; }
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string? Patronymic { get; set; }
        public string Login { get; set; } = "";
    }

    public class RequestItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Department { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public class RequestFull
    {
        public int RequestID { get; set; }
        public string RequestType { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string VisitPurpose { get; set; } = "";
        public string TargetDepartment { get; set; } = "";
        public string Note { get; set; } = "";
        public string VisitorLastName { get; set; } = "";
        public string VisitorFirstName { get; set; } = "";
        public string? VisitorPatronymic { get; set; }
        public string? VisitorPhone { get; set; }
        public string VisitorEmail { get; set; } = "";
        public string? VisitorOrganization { get; set; }
        public DateTime VisitorBirthDate { get; set; }
        public string VisitorPassportData { get; set; } = "";
    }

    public class AttachedFile
    {
        public int FileId { get; set; }
        public string FileType { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
    }

    public class Employee
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Login { get; set; } = "";
        public string Role { get; set; } = "";
        public string? Department { get; set; }
    }

    public class Guest
    {
        public Guid Id { get; set; }
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string? PassportSeries { get; set; }
        public string? PassportNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public string? Phone { get; set; }
        public string Email { get; set; } = "";
        public string? Organization { get; set; }
    }
}
