using System.Collections.Generic;

namespace DatwiseSafetyDemo.Models
{
    public sealed class DashboardMetrics
    {
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public int OverdueOpenCount { get; set; }

        public IDictionary<string, int> BySeverity { get; set; } = new Dictionary<string, int>();
        public IDictionary<string, int> ByType { get; set; } = new Dictionary<string, int>();
    }
}
