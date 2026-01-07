using System;

namespace DatwiseSafetyDemo.Models
{
    public class Hazard
    {
        public int HazardId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ReportedByUserId { get; set; }
        public int? AssignedToUserId { get; set; }
        public string Status { get; set; }      // Open / InProgress / Resolved
        public string Severity { get; set; }    // Low / Medium / High / Critical
        public string Type { get; set; }        // Fire / Electrical / ...
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public string ReportedByName { get; set; }
        public string AssignedToName { get; set; }
    }
}
