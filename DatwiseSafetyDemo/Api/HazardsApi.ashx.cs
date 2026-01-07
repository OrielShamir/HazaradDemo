using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Infrastructure;

namespace DatwiseSafetyDemo.Api
{
    [ExcludeFromCodeCoverage]
    public class HazardsApi : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            if (context == null) return;

            context.Response.ContentType = "application/json";

            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = 401;
                WriteJson(context, new { error = "Unauthorized" });
                return;
            }

            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                context.Response.StatusCode = 401;
                WriteJson(context, new { error = "Unauthorized" });
                return;
            }

            // Restrict internal API to management roles
            if (!string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, Roles.SiteManager, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                WriteJson(context, new { error = "Forbidden" });
                return;
            }

            var action = (context.Request["action"] ?? "health").Trim().ToLowerInvariant();
            var repo = new SqlHazardRepository();

            if (action == "health")
            {
                WriteJson(context, new { ok = true, timeUtc = DateTime.UtcNow });
                return;
            }

            if (action == "list")
            {
                var filter = new HazardFilter
                {
                    SearchText = context.Request["q"],
                    Status = context.Request["status"],
                    Severity = context.Request["severity"],
                    Type = context.Request["type"],
                    OverdueOnly = context.Request["overdue"] == "1"
                };

                var data = repo.GetHazards(filter, userId, role)
                    .Select(h => new
                    {
                        h.HazardId,
                        h.Title,
                        h.Status,
                        h.Severity,
                        h.Type,
                        h.ReportedByUserId,
                        h.ReportedByName,
                        h.AssignedToUserId,
                        h.AssignedToName,
                        CreatedAt = h.CreatedAt,
                        DueDate = h.DueDate
                    })
                    .ToList();

                WriteJson(context, new { items = data });
                return;
            }

            if (action == "detail")
            {
                if (!int.TryParse(context.Request["id"], out var id))
                {
                    context.Response.StatusCode = 400;
                    WriteJson(context, new { error = "Missing id" });
                    return;
                }

                var hazard = repo.GetById(id, userId, role);
                if (hazard == null)
                {
                    context.Response.StatusCode = 404;
                    WriteJson(context, new { error = "Not found" });
                    return;
                }

                var logs = repo.GetLogs(id, userId, role);

                WriteJson(context, new { hazard, logs });
                return;
            }

            if (action == "dashboard")
            {
                var metrics = repo.GetDashboardMetrics(userId, role);
                WriteJson(context, metrics);
                return;
            }

            context.Response.StatusCode = 400;
            WriteJson(context, new { error = "Unknown action" });
        }

        private static void WriteJson(HttpContext ctx, object obj)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
            ctx.Response.Write(serializer.Serialize(obj));
        }
    }
}
