using System;

namespace WpfApp2.Models
{
    public class Employee
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "user";
    }
}