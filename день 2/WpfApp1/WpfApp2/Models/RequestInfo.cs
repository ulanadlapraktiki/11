using System;

namespace WpfApp2.Models
{
    public class RequestInfo
    {
        public Guid Id { get; set; }
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string Status { get; set; } = "";
        public string GuestName { get; set; } = "";
        public string GuestEmail { get; set; } = "";
        public string GuestPhone { get; set; } = "";
    }
}