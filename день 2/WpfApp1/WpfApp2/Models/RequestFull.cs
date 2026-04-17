using System;

namespace WpfApp2.Models
{
    public class RequestFull
    {
        public Guid Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "";
        public string VisitPurpose { get; set; } = "";
        public string TargetDepartment { get; set; } = "";
        public string Note { get; set; } = "";
        public Guid GuestId { get; set; }
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string PassportSeries { get; set; } = "";
        public string PassportNumber { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Organization { get; set; } = "";
    }
}