using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DatwiseSafetyDemo.Models;

namespace DatwiseSafetyDemo.Services
{
    public static class HazardReportExporter
    {
        public static byte[] ExportCsv(IEnumerable<Hazard> hazards)
        {
            var sb = new StringBuilder();
            sb.AppendLine("HazardId,Title,Status,Severity,Type,ReportedBy,AssignedTo,CreatedAt,DueDate");

            foreach (var h in hazards ?? Enumerable.Empty<Hazard>())
            {
                sb.AppendLine(string.Join(",",
                    h.HazardId,
                    Csv(h.Title),
                    Csv(h.Status),
                    Csv(h.Severity),
                    Csv(h.Type),
                    Csv(h.ReportedByName),
                    Csv(h.AssignedToName),
                    h.CreatedAt.ToString("yyyy-MM-dd"),
                    h.DueDate.HasValue ? h.DueDate.Value.ToString("yyyy-MM-dd") : ""
                ));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] ExportPdf(IEnumerable<Hazard> hazards, string title)
        {
            var lines = new List<string>();

            foreach (var h in hazards ?? Enumerable.Empty<Hazard>())
            {
                var due = h.DueDate.HasValue ? h.DueDate.Value.ToString("yyyy-MM-dd") : "-";
                var assigned = string.IsNullOrWhiteSpace(h.AssignedToName) ? "-" : h.AssignedToName;
                lines.Add($"#{h.HazardId} | {h.Status} | {h.Severity} | {h.Type} | Due: {due} | Assigned: {assigned} | {h.Title}");
            }

            return SimplePdfWriter.CreateSinglePageTextPdf(lines, title);
        }

        private static string Csv(string s)
        {
            var v = s ?? string.Empty;
            // Escape quotes for CSV (" => "")
            v = v.Replace("\"", "\"\"");
            if (v.Contains(",") || v.Contains("\"") || v.Contains("\n") || v.Contains("\r"))
            {
                return "\"" + v + "\"";
            }
            return v;
        }
    }
}
