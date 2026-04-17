using System;

namespace WpfApp2.Models
{
    public class AccessLog
    {
        public int Id { get; set; }
        public Guid RequestId { get; set; }
        public DateTime AccessTime { get; set; }
        public string AccessType { get; set; } = "";
    }
}