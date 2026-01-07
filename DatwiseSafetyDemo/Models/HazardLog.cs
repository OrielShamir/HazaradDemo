using System;

namespace DatwiseSafetyDemo.Models
{
    public sealed class HazardLog
    {
        public int HazardLogId { get; set; }
        public int HazardId { get; set; }
        public string ActionType { get; set; }    // Created / Updated / Assigned / StatusChanged / Comment
        public string Details { get; set; }
        public int PerformedByUserId { get; set; }
        public string PerformedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
