using System;
using System.Collections.Generic;
using System.Text;

namespace BomProduction.Shared.Models
{
    public class ScheduleEntry
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty; // pending, in-progress, etc.
        public Guid AssignedTo { get; set; }
        public string Granularity { get; set; } = string.Empty; // day, month, year
    }
}
