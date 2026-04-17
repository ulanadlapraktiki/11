using System;

namespace WpfApp2.Models
{
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