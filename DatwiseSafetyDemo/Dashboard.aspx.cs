using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Infrastructure;

namespace DatwiseSafetyDemo
{
    [ExcludeFromCodeCoverage]
    public partial class Dashboard : SecurePage
    {
        protected override string[] AllowedRoles => new[] { Roles.FieldWorker, Roles.SiteManager, Roles.SafetyOfficer };

        private readonly IHazardRepository _hazards;

        public Dashboard()
        {
            _hazards = new SqlHazardRepository();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                try
                {
                    var metrics = _hazards.GetDashboardMetrics(userId, role);

                    lnkOpen.Text = metrics.OpenCount.ToString();
                    lnkOpen.NavigateUrl = ResolveUrl("~/Hazards/HazardList.aspx?status=Open");

                    lnkInProgress.Text = metrics.InProgressCount.ToString();
                    lnkInProgress.NavigateUrl = ResolveUrl("~/Hazards/HazardList.aspx?status=InProgress");

                    lnkResolved.Text = metrics.ResolvedCount.ToString();
                    lnkResolved.NavigateUrl = ResolveUrl("~/Hazards/HazardList.aspx?status=Resolved");

                    lnkOverdue.Text = metrics.OverdueOpenCount.ToString();
                    lnkOverdue.NavigateUrl = ResolveUrl("~/Hazards/HazardList.aspx?overdue=1");

                    BindBreakdown(gvBySeverity, metrics.BySeverity, "severity");
                    BindBreakdown(gvByType, metrics.ByType, "type");
                }
                catch (Exception ex)
                {
                    pnlError.Visible = true;
                    litError.Text = HttpUtility.HtmlEncode(ex.Message);
                }
            }
        }

        private void BindBreakdown(System.Web.UI.WebControls.GridView grid, IDictionary<string, int> data, string queryKey)
        {
            var max = data.Count == 0 ? 1 : Math.Max(1, data.Values.Max());

            var rows = data
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new
                {
                    Key = kv.Key,
                    Count = kv.Value,
                    Percent = (int)Math.Round((kv.Value / (double)max) * 100),
                    Link = ResolveUrl("~/Hazards/HazardList.aspx?" + queryKey + "=" + HttpUtility.UrlEncode(kv.Key))
                })
                .ToList();

            grid.DataSource = rows;
            grid.DataBind();
        }
    }
}
