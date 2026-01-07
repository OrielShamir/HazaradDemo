namespace DatwiseSafetyDemo.Services
{
    public class HazardClassificationResult
    {
        public string Severity { get; set; }
        public string Type { get; set; }
    }

    public interface IHazardClassificationService
    {
        HazardClassificationResult Classify(string description);
    }

    /// <summary>
    /// Heuristic-only "AI assist" placeholder.
    /// IMPORTANT: Must return values that exist in the UI dropdowns.
    /// </summary>
    public class StubHazardClassificationService : IHazardClassificationService
    {
        public HazardClassificationResult Classify(string description)
        {
            var result = new HazardClassificationResult
            {
                Severity = "Medium",
                Type = "General"
            };

            if (string.IsNullOrWhiteSpace(description))
            {
                return result;
            }

            var text = description.ToLowerInvariant();

            if (text.Contains("fire") || text.Contains("אש") || text.Contains("smoke") || text.Contains("עשן"))
            {
                result.Type = "Fire";
                result.Severity = "High";
                return result;
            }

            if (text.Contains("electric") || text.Contains("electrical") || text.Contains("חשמל") || text.Contains("קצר"))
            {
                result.Type = "Electrical";
                result.Severity = "High";
                return result;
            }

            if (text.Contains("chemical") || text.Contains("acid") || text.Contains("gas") || text.Contains("fume") ||
                text.Contains("כימ") || text.Contains("חומצה") || text.Contains("גז") || text.Contains("אדים"))
            {
                result.Type = "Chemical";
                result.Severity = "Critical";
                return result;
            }

            if (text.Contains("machine") || text.Contains("equipment") || text.Contains("forklift") ||
                text.Contains("מכונה") || text.Contains("ציוד") || text.Contains("מלגזה"))
            {
                result.Type = "Equipment";
                result.Severity = "High";
                return result;
            }

            return result;
        }
    }
}
