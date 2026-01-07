using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Infrastructure;
using DatwiseSafetyDemo.Services;

namespace DatwiseSafetyDemo.Reports
{
    [ExcludeFromCodeCoverage]
    public class HazardsReport : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            if (context == null) return;

            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = 401;
                return;
            }

            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var format = (context.Request["format"] ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid format");
                return;
            }

            var filter = new HazardFilter
            {
                SearchText = context.Request["q"],
                Status = context.Request["status"],
                Severity = context.Request["severity"],
                Type = context.Request["type"],
                OverdueOnly = context.Request["overdue"] == "1"
            };

            if (int.TryParse(context.Request["assignedTo"], out var assignedTo))
            {
                filter.AssignedToUserId = assignedTo;
            }

            var repo = new SqlHazardRepository();
            var hazards = repo.GetHazards(filter, userId, role);

            var fileName = $"hazards_{DateTime.UtcNow:yyyyMMdd_HHmm}.{format}";
            byte[] bytes;

            if (format == "pdf")
            {
                bytes = HazardReportExporter.ExportPdf(hazards, "Hazards Report");
                context.Response.ContentType = "application/pdf";
            }
            else
            {
                bytes = HazardReportExporter.ExportCsv(hazards);
                context.Response.ContentType = "text/csv";
            }

            context.Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            context.Response.BinaryWrite(bytes);
            context.Response.End();
        }
    }
}
