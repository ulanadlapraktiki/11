using System;

namespace WpfApp2.Models
{
    public class Request
    {
        public Guid Id { get; set; }
        public Guid GuestId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string VisitPurpose { get; set; } = "";
        public string TargetDepartment { get; set; } = "";
        public string Note { get; set; } = "";
        public string Status { get; set; } = "проверка";
    }
}