using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DatwiseSafetyDemo.Data;
using DatwiseSafetyDemo.Infrastructure;
using DatwiseSafetyDemo.Models;

namespace DatwiseSafetyDemo.Hazards
{
    [ExcludeFromCodeCoverage]
    public partial class HazardList : SecurePage
    {
        protected override string[] AllowedRoles => new[] { Roles.FieldWorker, Roles.SiteManager, Roles.SafetyOfficer };

        private readonly IHazardRepository _hazards;

        public HazardList()
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
                PopulateStaticFilters();
                ConfigureRoleBasedFilters(role);
                ApplyQueryStringFilters();
                BindGrid(userId, role);
                }
        }

        protected void btnFilter_OnClick(object sender, EventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            BindGrid(userId, role);
        }

        protected void btnClear_OnClick(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            ddlStatus.SelectedValue = string.Empty;
            ddlSeverity.SelectedValue = string.Empty;
            ddlType.SelectedValue = string.Empty;
            chkOverdue.Checked = false;

            if (ddlAssignedTo != null)
            {
                ddlAssignedTo.SelectedValue = string.Empty;
            }

            if (CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                BindGrid(userId, role);
            }
        }

        protected void btnNewHazard_OnClick(object sender, EventArgs e)
        {
            Response.Redirect("HazardEdit.aspx");
        }

        protected void gvHazards_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!CurrentUser.TryGet(out var userId, out var role, out var fullName))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!int.TryParse(e.CommandArgument as string, out var hazardId))
            {
                return;
            }

            // Load hazard with RBAC scoping; null => no access
            var hazard = _hazards.GetById(hazardId, userId, role);
            if (hazard == null)
            {
                Response.Redirect("~/Account/AccessDenied.aspx");
                return;
            }

            if (e.CommandName == "Take")
            {
                if (!HazardAuthorization.CanSelfAssign(role, userId, hazard))
                {
                    Response.Redirect("~/Account/AccessDenied.aspx");
                    return;
                }

                _hazards.Assign(hazardId, userId, userId);
            }
            else if (e.CommandName == "Start")
            {
                if (!HazardAuthorization.CanChangeStatus(role, userId, hazard, "InProgress"))
                {
                    Response.Redirect("~/Account/AccessDenied.aspx");
                    return;
                }

                _hazards.ChangeStatus(hazardId, "InProgress", userId, "Started work");
            }
            else if (e.CommandName == "Resolve")
            {
                if (!HazardAuthorization.CanChangeStatus(role, userId, hazard, "Resolved"))
                {
                    Response.Redirect("~/Account/AccessDenied.aspx");
                    return;
                }

                _hazards.ChangeStatus(hazardId, "Resolved", userId, "Marked as resolved");
            }

            BindGrid(userId, role);
        }

        protected void gvHazards_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var hazard = (Hazard)e.Row.DataItem;
            if (hazard.DueDate.HasValue && hazard.DueDate.Value.Date < DateTime.Today && !string.Equals(hazard.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
            {
                e.Row.CssClass = (e.Row.CssClass + " danger").Trim();
            }

            var role = (ViewState["Role"] as string) ?? string.Empty;
            var userId = (int?)ViewState["UserId"] ?? 0;

            var btnTake = e.Row.FindControl("btnTake") as LinkButton;
            var btnStart = e.Row.FindControl("btnStart") as LinkButton;
            var btnResolve = e.Row.FindControl("btnResolve") as LinkButton;

            if (btnTake != null)
            {
                btnTake.Visible = HazardAuthorization.CanSelfAssign(role, userId, hazard);
            }

            if (btnStart != null)
            {
                btnStart.Visible = HazardAuthorization.CanChangeStatus(role, userId, hazard, "InProgress");
            }

            if (btnResolve != null)
            {
                btnResolve.Visible = HazardAuthorization.CanChangeStatus(role, userId, hazard, "Resolved");
            }
        }

        private void BindGrid(int userId, string role)
        {
            ViewState["UserId"] = userId;
            ViewState["Role"] = role;

            var filter = BuildFilter(role);
            var hazards = _hazards.GetHazards(filter, userId, role);

            gvHazards.DataSource = hazards;
            gvHazards.DataBind();

            ConfigureExports(filter, role);
        }

        private HazardFilter BuildFilter(string role)
        {
            var filter = new HazardFilter
            {
                SearchText = string.IsNullOrWhiteSpace(txtSearch.Text) ? null : txtSearch.Text.Trim(),
                Status = string.IsNullOrEmpty(ddlStatus.SelectedValue) ? null : ddlStatus.SelectedValue,
                Severity = string.IsNullOrEmpty(ddlSeverity.SelectedValue) ? null : ddlSeverity.SelectedValue,
                Type = string.IsNullOrEmpty(ddlType.SelectedValue) ? null : ddlType.SelectedValue,
                OverdueOnly = chkOverdue.Checked
            };

            if (ddlAssignedTo != null && string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(ddlAssignedTo.SelectedValue, out var assignedTo))
                {
                    filter.AssignedToUserId = assignedTo;
                }
            }

            return filter;
        }

        private void ConfigureExports(HazardFilter filter, string role)
        {
            var baseUrl = ResolveUrl("~/Reports/HazardsReport.ashx");
            lnkExportCsv.NavigateUrl = baseUrl + BuildQueryString(filter, "csv");
            lnkExportPdf.NavigateUrl = baseUrl + BuildQueryString(filter, "pdf");
        }

        private string BuildQueryString(HazardFilter filter, string format)
        {
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["format"] = format;

            if (!string.IsNullOrWhiteSpace(filter.SearchText)) qs["q"] = filter.SearchText;
            if (!string.IsNullOrWhiteSpace(filter.Status)) qs["status"] = filter.Status;
            if (!string.IsNullOrWhiteSpace(filter.Severity)) qs["severity"] = filter.Severity;
            if (!string.IsNullOrWhiteSpace(filter.Type)) qs["type"] = filter.Type;
            if (filter.AssignedToUserId.HasValue) qs["assignedTo"] = filter.AssignedToUserId.Value.ToString();
            if (filter.OverdueOnly) qs["overdue"] = "1";

            return "?" + qs.ToString();
        }

        private void PopulateStaticFilters()
        {
            // Status
            ddlStatus.Items.Clear();
            ddlStatus.Items.Add(new ListItem("All", ""));
            ddlStatus.Items.Add(new ListItem("Open", "Open"));
            ddlStatus.Items.Add(new ListItem("In Progress", "InProgress"));
            ddlStatus.Items.Add(new ListItem("Resolved", "Resolved"));

            // Severity
            ddlSeverity.Items.Clear();
            ddlSeverity.Items.Add(new ListItem("All", ""));
            ddlSeverity.Items.Add(new ListItem("Low", "Low"));
            ddlSeverity.Items.Add(new ListItem("Medium", "Medium"));
            ddlSeverity.Items.Add(new ListItem("High", "High"));
            ddlSeverity.Items.Add(new ListItem("Critical", "Critical"));

            // Type
            ddlType.Items.Clear();
            ddlType.Items.Add(new ListItem("All", ""));
            ddlType.Items.Add(new ListItem("Fire", "Fire"));
            ddlType.Items.Add(new ListItem("Electrical", "Electrical"));
            ddlType.Items.Add(new ListItem("Chemical", "Chemical"));
            ddlType.Items.Add(new ListItem("Equipment", "Equipment"));
            ddlType.Items.Add(new ListItem("Physical", "Physical"));
            ddlType.Items.Add(new ListItem("General", "General"));
        }

        private void ConfigureRoleBasedFilters(string role)
        {
            pnlAssignedTo.Visible = string.Equals(role, Roles.SafetyOfficer, StringComparison.OrdinalIgnoreCase);

            if (pnlAssignedTo.Visible)
            {
                // Safety officer can filter by assigned site manager
                ddlAssignedTo.Items.Clear();
                ddlAssignedTo.Items.Add(new ListItem("All", ""));

                var usersRepo = new SqlUserRepository();
                var managers = usersRepo.GetActiveUsersByRole(Roles.SiteManager);

                foreach (var u in managers.OrderBy(x => x.FullName))
                {
                    ddlAssignedTo.Items.Add(new ListItem(u.FullName, u.UserId.ToString()));
                }
            }
        }

        private void ApplyQueryStringFilters()
        {
            // Enables drill-down from dashboard.
            txtSearch.Text = Request.QueryString["q"] ?? string.Empty;

            var status = Request.QueryString["status"];
            if (!string.IsNullOrWhiteSpace(status))
                ddlStatus.SelectedValue = status;

            var severity = Request.QueryString["severity"];
            if (!string.IsNullOrWhiteSpace(severity))
                ddlSeverity.SelectedValue = severity;

            var type = Request.QueryString["type"];
            if (!string.IsNullOrWhiteSpace(type) && ddlType.Items.FindByValue(type) != null)
                ddlType.SelectedValue = type;

            chkOverdue.Checked = Request.QueryString["overdue"] == "1";

            var assignedTo = Request.QueryString["assignedTo"];
            if (!string.IsNullOrWhiteSpace(assignedTo) && ddlAssignedTo != null)
            {
                ddlAssignedTo.SelectedValue = assignedTo;
            }
        }
    }
}
